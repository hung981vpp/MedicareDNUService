using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace PharmacyBillingService.Messaging
{
    public static class RabbitMqConnectionFactory
    {
        public static RabbitMqOptions GetOptions(IConfiguration configuration)
            => configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        public static IConnection CreateConnection(IConfiguration configuration)
        {
            var options = GetOptions(configuration);
            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };

            return factory.CreateConnection();
        }
    }
}
