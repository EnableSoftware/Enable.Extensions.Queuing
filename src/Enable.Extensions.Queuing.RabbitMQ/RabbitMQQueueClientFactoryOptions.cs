namespace Enable.Extensions.Queuing.RabbitMQ
{
    public class RabbitMQQueueClientFactoryOptions
    {
        public string HostName { get; set; }

        public int Port { get; set; }

        public string VirtualHost { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether queues should be put into "lazy" mode as opposed to "default" mode.
        /// See <a href="https://www.rabbitmq.com/lazy-queues.html">RabbitMQ documentation on Lazy Queues</a>.
        /// </summary>
        public bool LazyQueues { get; set; }
    }
}
