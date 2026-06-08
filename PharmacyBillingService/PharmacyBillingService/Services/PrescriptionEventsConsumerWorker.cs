using System.Text;
using System.Text.Json;
using PharmacyBillingService.Events;
using PharmacyBillingService.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PharmacyBillingService.Services
{
    public sealed class PrescriptionEventsConsumerWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PrescriptionEventsConsumerWorker> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private IConnection? _connection;
        private IModel? _channel;

        public PrescriptionEventsConsumerWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<PrescriptionEventsConsumerWorker> logger)
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
                    _logger.LogError(ex, "N3 RabbitMQ prescription consumer failed. Retrying in 5 seconds.");
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
            _channel.QueueDeclare(options.PrescriptionQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(options.PrescriptionQueue, options.Exchange, "prescription.created");
            _channel.BasicQos(0, 1, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += HandleMessageAsync;
            _channel.BasicConsume(options.PrescriptionQueue, autoAck: false, consumer);

            _logger.LogInformation("N3 RabbitMQ consumer started on queue {Queue}.", options.PrescriptionQueue);
        }

        private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            if (_channel is null) return;

            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var ev = JsonSerializer.Deserialize<PrescriptionCreatedEvent>(payload, _jsonOptions)
                    ?? throw new InvalidOperationException("Invalid prescription.created payload");

                using var scope = _serviceProvider.CreateScope();
                var prescriptionService = scope.ServiceProvider.GetRequiredService<IPrescriptionService>();
                await prescriptionService.ProcessPrescriptionCreatedEventAsync(ev);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("N3 consumed RabbitMQ event prescription.created for PrescriptionId {PrescriptionId}.", ev.PrescriptionId);
            }
            catch (Exception ex)
            {
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                _logger.LogError(ex, "N3 failed to consume RabbitMQ message {RoutingKey}.", ea.RoutingKey);
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
}
