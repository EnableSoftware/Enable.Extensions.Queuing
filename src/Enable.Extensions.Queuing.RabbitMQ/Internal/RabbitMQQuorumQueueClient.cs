using System.Collections.Generic;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQuorumQueueClient : BaseRabbitMQQueueClient
    {
        public RabbitMQQuorumQueueClient(
            ConnectionFactory connectionFactory,
            string queueName,
            QueueMode queueMode = QueueMode.Default,
            string deadLetterQueueName = null)
            : base(connectionFactory, queueName, queueMode, deadLetterQueueName)
        {
            QueueArguments.Add("x-queue-type", "quorum");
            DLQueueArguments = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" },
                };

            DeclareQueues();
        }
    }
}
