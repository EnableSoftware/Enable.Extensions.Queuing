using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.RabbitMQ.Internal;

namespace Enable.Extensions.Queuing.RabbitMQ
{
    public class RabbitMQQuorumQueueClientFactory : BaseRabbitMQQueueClientFactory, IQueueClientFactory
    {
        public RabbitMQQuorumQueueClientFactory(RabbitMQQueueClientFactoryOptions options)
            : base(options)
        {
        }

        public IQueueClient GetQueueReference(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new RabbitMQQuorumQueueClient(ConnectionFactory, queueName, QueueMode);
        }
    }
}
