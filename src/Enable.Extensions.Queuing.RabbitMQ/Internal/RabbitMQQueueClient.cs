using Enable.Extensions.Queuing.Abstractions;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQueueClient : BaseRabbitMQQueueClient
    {
        public RabbitMQQueueClient(
            ConnectionFactory connectionFactory,
            string queueName,
            QueueMode queueMode = QueueMode.Default,
            QueueOptions queueOptions = null)
            : base(connectionFactory, queueName, queueMode, queueOptions)
        {
            DeclareQueues();
        }
    }
}
