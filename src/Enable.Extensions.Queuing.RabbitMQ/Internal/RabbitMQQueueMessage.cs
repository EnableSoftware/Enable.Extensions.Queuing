using Enable.Extensions.Queuing.Abstractions;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQueueMessage : IQueueMessage
    {
        public RabbitMQQueueMessage(BasicGetResult result)
        {
            Body = result.Body;
            LeaseId = result.DeliveryTag.ToString();
            MessageId = result.BasicProperties.MessageId;

            // Storing a delivery count is on the roadmap for RabbitMQ 3.8, see
            // https://github.com/rabbitmq/rabbitmq-server/issues/502 for more
            // information. In the meantime, dead letter messages on the first
            // negative acknowledgement (see
            // `RabbitMQQueueClient.AbandonAsync()`). Therefore, we know that
            // `DequeueCount` will always be one.
            DequeueCount = 1;
        }

        public byte[] Body { get; }

        public uint DequeueCount { get; }

        public string LeaseId { get; }

        public string MessageId { get; }
    }
}
