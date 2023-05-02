using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQueueClient : BaseRabbitMQQueueClient
    {
        public RabbitMQQueueClient(
            ConnectionFactory connectionFactory,
            string queueName,
            QueueMode queueMode = QueueMode.Default,
            string deadLetterQueueName = null)
            : base(connectionFactory, queueName, queueMode, deadLetterQueueName)
        {
            DeclareQueues();
        }
    }
}
