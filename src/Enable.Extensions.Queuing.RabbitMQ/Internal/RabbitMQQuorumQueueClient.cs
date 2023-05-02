using System.Collections.Generic;
using Enable.Extensions.Queuing.Abstractions;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQuorumQueueClient : BaseRabbitMQQueueClient
    {
        public RabbitMQQuorumQueueClient(
            ConnectionFactory connectionFactory,
            string queueName,
            QueueMode queueMode = QueueMode.Default,
            QueueOptions queueOptions = null)
            : base(connectionFactory, queueName, queueMode, queueOptions)
        {
            QueueArguments.Add("x-queue-type", "quorum");

            if (DLQueueArguments != null)
            {
                DLQueueArguments.Add("x-queue-type", "quorum");
            }
            else
            {
                DLQueueArguments = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" },
                };
            }

            DeclareQueues();
        }
    }
}
