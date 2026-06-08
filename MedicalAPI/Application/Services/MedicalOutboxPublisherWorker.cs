using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MedicalAPI.Application.Messaging;
using MedicalAPI.Domain.Constants;
using MedicalAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MedicalAPI.Application.Services;

public sealed class MedicalOutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicalOutboxPublisherWorker> _logger;
    private readonly IConfiguration _configuration;

    public MedicalOutboxPublisherWorker(
        IServiceProvider serviceProvider,
        ILogger<MedicalOutboxPublisherWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MedicalOutboxPublisherWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing N2 outbox events.");
            }

            await Task.Delay(3000, stoppingToken);
        }

        _logger.LogInformation("MedicalOutboxPublisherWorker stopped.");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();

        var pendingEvents = await db.OutboxEvents
            .Where(e => e.Status == MedicalStatuses.PendingPublish && e.RetryCount < 5)
            .OrderBy(e => e.OccurredAt)
            .Take(10)
            .ToListAsync(stoppingToken);

        if (!pendingEvents.Any())
        {
            return;
        }

        _logger.LogInformation("N2 Outbox found {Count} pending events to publish to N3.", pendingEvents.Count);

        var rabbitOptions = RabbitMqConnectionFactory.GetOptions(_configuration);
        using var connection = RabbitMqConnectionFactory.CreateConnection(_configuration);
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(rabbitOptions.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(rabbitOptions.PrescriptionQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(rabbitOptions.PrescriptionQueue, rabbitOptions.Exchange, "prescription.created");

        foreach (var ev in pendingEvents)
        {
            var routingKey = ev.EventType switch
            {
                "prescription.created" => "prescription.created",
                _ => null
            };

            if (routingKey is null)
            {
                _logger.LogWarning("Unsupported event type {EventType} for Outbox Event {Id}", ev.EventType, ev.Id);
                ev.Status = "Thất bại"; // Mark as Failed in Vietnamese
                ev.ErrorMessage = $"Unsupported event type: {ev.EventType}";
                await db.SaveChangesAsync(stoppingToken);
                continue;
            }

            try
            {
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

                ev.Status = MedicalStatuses.Published;
                ev.PublishedAt = DateTime.UtcNow;
                _logger.LogInformation("Published N2 event {EventCode} ({EventType}) to RabbitMQ exchange {Exchange} with routing key {RoutingKey}.",
                    ev.EventCode, ev.EventType, rabbitOptions.Exchange, routingKey);
            }
            catch (Exception ex)
            {
                ev.RetryCount++;
                ev.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Exception while publishing N2 event {EventCode}", ev.EventCode);
            }

            if (ev.RetryCount >= 5 && ev.Status != MedicalStatuses.Published)
            {
                ev.Status = "Thất bại";
            }

            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
