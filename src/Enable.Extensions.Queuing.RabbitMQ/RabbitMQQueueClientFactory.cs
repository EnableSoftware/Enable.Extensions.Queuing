using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.RabbitMQ.Internal;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ
{
    public class RabbitMQQueueClientFactory : IQueueClientFactory
    {
        private readonly RabbitMQConnectionManager _connectionManager;
        private readonly QueueMode _queueMode;

        public RabbitMQQueueClientFactory(RabbitMQQueueClientFactoryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _connectionManager = new RabbitMQConnectionManager(options);

            _queueMode = options.LazyQueues ? QueueMode.Lazy : QueueMode.Default;
        }

        public IQueueClient GetQueueReference(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new RabbitMQQueueClient(_connectionManager, queueName, _queueMode);
        }
    }
}
