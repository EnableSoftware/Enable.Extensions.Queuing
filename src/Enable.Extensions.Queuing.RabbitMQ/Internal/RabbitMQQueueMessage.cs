using Enable.Extensions.Queuing.Abstractions;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQueueMessage : BaseQueueMessage
    {
        public RabbitMQQueueMessage(
            byte[] body,
            ulong deliveryTag,
            IBasicProperties properties)
        {
            Body = body;
            LeaseId = deliveryTag.ToString();
            MessageId = properties.MessageId;

            // Storing a delivery count is on the roadmap for RabbitMQ 3.8, see
            // https://github.com/rabbitmq/rabbitmq-server/issues/502 for more
            // information. In the meantime, dead letter messages on the first
            // negative acknowledgement (see
            // `RabbitMQQueueClient.AbandonAsync()`). Therefore, we know that
            // `DequeueCount` will always be one.
            DequeueCount = 1;
        }

        public override byte[] Body { get; }

        public override uint DequeueCount { get; }

        public override string LeaseId { get; }

        public override string MessageId { get; }
    }
}
