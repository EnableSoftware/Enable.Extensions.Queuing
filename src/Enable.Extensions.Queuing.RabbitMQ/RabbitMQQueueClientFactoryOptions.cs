using System;

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
        ///  Gets or sets a value indicating whether automatic connection recovery is enabled. Defaults to true.
        /// </summary>
        public bool AutomaticRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the amount of time the client will wait before re-trying to recover the connection. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets a value indicating whether queues should be put into "lazy" mode as opposed to "default" mode.
        /// See <a href="https://www.rabbitmq.com/lazy-queues.html">RabbitMQ documentation on Lazy Queues</a>.
        /// </summary>
        public bool LazyQueues { get; set; }
    }
}
