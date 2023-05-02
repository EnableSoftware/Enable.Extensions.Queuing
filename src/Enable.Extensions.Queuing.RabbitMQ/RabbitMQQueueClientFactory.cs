using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.RabbitMQ.Internal;

namespace Enable.Extensions.Queuing.RabbitMQ
{
    public class RabbitMQQueueClientFactory : BaseRabbitMQQueueClientFactory, IQueueClientFactory
    {
        public RabbitMQQueueClientFactory(RabbitMQQueueClientFactoryOptions options)
            : base(options)
        {
        }

        public IQueueClient GetQueueReference(string queueName, string deadLetterQueueName = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new RabbitMQQueueClient(ConnectionFactory, queueName, QueueMode, deadLetterQueueName);
        }
    }
}
