namespace Enable.Extensions.Queuing.Abstractions
{
    public class QueueOptions
    {
        /// <summary>
        /// Gets or sets the dead letter queue name, if not providied the default value will be used.
        /// Applicable to RabbitMQ & Azure storage queues.
        /// </summary>
        public string DeadLetterQueueName { get; set; }

        /// <summary>
        /// Gets or sets the x-message-ttl for RabbitMQ queues, if not providied the default value will be used.
        /// Applicable to RabbitMQ queues.
        /// </summary>
        public int? DeadLetterQueueTtlMs { get; set; }

        /// <summary>
        /// Gets or sets the x-dead-letter-exchange for the dead letter queue.
        /// If not providied the value will not be set.
        /// Applicable to RabbitMQ queues.
        /// </summary>
        public string DeadLetterExchange { get; set; }

        /// <summary>
        /// Gets or sets the x-dead-letter-routing-key for the dead letter queue.
        /// If not providied the value will not be set.
        /// Applicable to RabbitMQ queues.
        /// </summary>
        public string DeadLetterRoutingKey { get; set; }
    }
}
