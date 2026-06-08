using System.Text;
using System.Text.Json;
using MedicalAPI.Application.DTOs;
using MedicalAPI.Application.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MedicalAPI.Application.Services;

public sealed class AppointmentEventsConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppointmentEventsConsumerWorker> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private IConnection? _connection;
    private IModel? _channel;

    public AppointmentEventsConsumerWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AppointmentEventsConsumerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsumer();
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "N2 RabbitMQ appointment consumer failed. Retrying in 5 seconds.");
                DisposeRabbitMq();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private void StartConsumer()
    {
        var options = RabbitMqConnectionFactory.GetOptions(_configuration);
        _connection = RabbitMqConnectionFactory.CreateConnection(_configuration);
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(options.AppointmentQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(options.AppointmentQueue, options.Exchange, "appointment.confirmed");
        _channel.QueueBind(options.AppointmentQueue, options.Exchange, "patient.checked_in");
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;
        _channel.BasicConsume(options.AppointmentQueue, autoAck: false, consumer);

        _logger.LogInformation("N2 RabbitMQ consumer started on queue {Queue}.", options.AppointmentQueue);
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
        var routingKey = ea.RoutingKey;

        if (string.IsNullOrWhiteSpace(payload))
        {
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            _logger.LogWarning("N2 dropped RabbitMQ event {RoutingKey} because payload is empty.", routingKey);
            await Task.CompletedTask;
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IMedicalRecordService>();

            var result = routingKey switch
            {
                "appointment.confirmed" => service.HandleAppointmentConfirmed(
                    JsonSerializer.Deserialize<AppointmentConfirmedEventRequest>(payload, _jsonOptions)
                    ?? throw new InvalidOperationException("Invalid appointment.confirmed payload")),
                "patient.checked_in" => service.HandlePatientCheckedIn(
                    JsonSerializer.Deserialize<PatientCheckedInEventRequest>(payload, _jsonOptions)
                    ?? throw new InvalidOperationException("Invalid patient.checked_in payload")),
                _ => throw new InvalidOperationException($"Unsupported routing key: {routingKey}")
            };

            if (result.IsSuccess)
            {
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("N2 consumed RabbitMQ event {RoutingKey}: {Message}", routingKey, result.Message);
            }
            else
            {
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                _logger.LogWarning("N2 rejected RabbitMQ event {RoutingKey}: {Message}", routingKey, result.Message);
            }
        }
        catch (JsonException ex)
        {
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            _logger.LogWarning(ex, "N2 dropped RabbitMQ event {RoutingKey} because payload is invalid JSON.", routingKey);
        }
        catch (InvalidOperationException ex)
        {
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            _logger.LogWarning(ex, "N2 dropped unsupported or invalid RabbitMQ event {RoutingKey}.", routingKey);
        }
        catch (Exception ex)
        {
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            _logger.LogError(ex, "N2 failed to consume RabbitMQ message {RoutingKey}.", routingKey);
        }

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        DisposeRabbitMq();
        base.Dispose();
    }

    private void DisposeRabbitMq()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
    }
}
