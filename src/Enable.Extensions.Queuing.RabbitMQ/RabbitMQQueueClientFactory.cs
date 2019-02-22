using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.RabbitMQ.Internal;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ
{
    public class RabbitMQQueueClientFactory : IQueueClientFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly QueueMode _queueMode;

        public RabbitMQQueueClientFactory(RabbitMQQueueClientFactoryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

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

            _queueMode = options.LazyQueues ? QueueMode.Lazy : QueueMode.Default;
        }

        public IQueueClient GetQueueReference(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new RabbitMQQueueClient(_connectionFactory, queueName, _queueMode);
        }
    }
}
