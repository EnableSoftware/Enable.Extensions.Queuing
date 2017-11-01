using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.RabbitMQ.Internal;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ
{
    public class RabbitMQQueueClientFactory : IQueueClientFactory
    {
        private readonly ConnectionFactory _connectionFactory;

        public RabbitMQQueueClientFactory(RabbitMQQueueClientFactoryOptions options)
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
        }

        public IQueueClient GetQueueReference(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new RabbitMQQueueClient(_connectionFactory, queueName);
        }
    }
}
