using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppointmentService.Data;
using AppointmentService.Messaging;
using AppointmentService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AppointmentService.Services;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private readonly IConfiguration _configuration;

    public OutboxPublisherWorker(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisherWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events in background worker.");
            }

            await Task.Delay(3000, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisherWorker stopped.");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();

        var pendingEvents = await dbContext.OutboxEvents
            .Where(e => e.Status == "Pending" && e.RetryCount < 5)
            .OrderBy(e => e.OccurredAt)
            .Take(10)
            .ToListAsync(stoppingToken);

        if (!pendingEvents.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} pending outbox events to publish.", pendingEvents.Count);

        var rabbitOptions = RabbitMqConnectionFactory.GetOptions(_configuration);
        using var connection = RabbitMqConnectionFactory.CreateConnection(_configuration);
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(rabbitOptions.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(rabbitOptions.AppointmentQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(rabbitOptions.AppointmentQueue, rabbitOptions.Exchange, "appointment.created");
        channel.QueueBind(rabbitOptions.AppointmentQueue, rabbitOptions.Exchange, "appointment.confirmed");
        channel.QueueBind(rabbitOptions.AppointmentQueue, rabbitOptions.Exchange, "patient.checked_in");
        channel.QueueBind(rabbitOptions.AppointmentQueue, rabbitOptions.Exchange, "appointment.cancelled");
        channel.QueueBind(rabbitOptions.AppointmentQueue, rabbitOptions.Exchange, "appointment.started");
        channel.QueueBind(rabbitOptions.AppointmentQueue, rabbitOptions.Exchange, "appointment.completed");

        foreach (var ev in pendingEvents)
        {
            var routingKey = ev.EventType switch
            {
                "appointment.created" => "appointment.created",
                "appointment.confirmed" => "appointment.confirmed",
                "patient.checked_in" => "patient.checked_in",
                "appointment.cancelled" => "appointment.cancelled",
                "appointment.started" => "appointment.started",
                "appointment.completed" => "appointment.completed",
                _ => null
            };

            if (routingKey is null)
            {
                _logger.LogWarning("Unsupported event type {EventType} for Outbox Event {Id}", ev.EventType, ev.Id);
                ev.Status = "Failed";
                ev.ErrorMessage = $"Unsupported event type: {ev.EventType}";
                await dbContext.SaveChangesAsync(stoppingToken);
                continue;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(ev.Payload))
                {
                    ev.Status = "Failed";
                    ev.ErrorMessage = "Outbox payload is empty.";
                    _logger.LogWarning("Skipped outbox event {EventCode} ({EventType}) because payload is empty.", ev.EventCode, ev.EventType);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                try
                {
                    using var _ = JsonDocument.Parse(ev.Payload);
                }
                catch (JsonException ex)
                {
                    ev.Status = "Failed";
                    ev.ErrorMessage = $"Invalid JSON payload: {ex.Message}";
                    _logger.LogWarning(ex, "Skipped outbox event {EventCode} ({EventType}) because payload is invalid JSON.", ev.EventCode, ev.EventType);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                var body = Encoding.UTF8.GetBytes(ev.Payload);
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = ev.EventCode;
                properties.Type = ev.EventType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                channel.BasicPublish(
                    exchange: rabbitOptions.Exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                ev.Status = "Processed";
                ev.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Published outbox event {EventCode} ({EventType}) to RabbitMQ exchange {Exchange} with routing key {RoutingKey}.",
                    ev.EventCode, ev.EventType, rabbitOptions.Exchange, routingKey);
            }
            catch (Exception ex)
            {
                ev.RetryCount++;
                ev.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Exception while publishing outbox event {EventCode}", ev.EventCode);
            }

            if (ev.RetryCount >= 5 && ev.Status != "Processed")
            {
                ev.Status = "Failed";
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
