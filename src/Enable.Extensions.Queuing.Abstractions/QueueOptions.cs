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
        public int? DeadLettQueueTtlMs { get; set; }
    }
}
