using System;
using Enable.Extensions.Queuing.RabbitMQ.Internal;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ
{
    public class BaseRabbitMQQueueClientFactory
    {
        public BaseRabbitMQQueueClientFactory(RabbitMQQueueClientFactoryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ConnectionFactory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            };

            QueueMode = options.LazyQueues ? QueueMode.Lazy : QueueMode.Default;
        }

        internal ConnectionFactory ConnectionFactory { get; set; }
        internal QueueMode QueueMode { get; set; }
    }
}
