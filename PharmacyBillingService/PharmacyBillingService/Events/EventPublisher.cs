using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PharmacyBillingService.Messaging;
using RabbitMQ.Client;

namespace PharmacyBillingService.Events
{
    public class EventPublisher : IEventPublisher
    {
        private readonly ILogger<EventPublisher> _logger;
        private readonly IConfiguration _configuration;

        public EventPublisher(ILogger<EventPublisher> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task PublishAsync<T>(string eventName, T eventData)
        {
            var jsonString = JsonSerializer.Serialize(eventData, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("================================================================================");
            _logger.LogInformation("PUBLISHING EVENT: {EventName}", eventName);
            _logger.LogInformation("Payload:\n{Payload}", jsonString);
            _logger.LogInformation("================================================================================");

            var rabbitOptions = RabbitMqConnectionFactory.GetOptions(_configuration);
            using var connection = RabbitMqConnectionFactory.CreateConnection(_configuration);
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(rabbitOptions.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = eventName;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: rabbitOptions.Exchange,
                routingKey: eventName,
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(jsonString));

            _logger.LogInformation("Published event {EventName} to RabbitMQ exchange {Exchange}.", eventName, rabbitOptions.Exchange);
            
            return Task.CompletedTask;
        }
    }
}
