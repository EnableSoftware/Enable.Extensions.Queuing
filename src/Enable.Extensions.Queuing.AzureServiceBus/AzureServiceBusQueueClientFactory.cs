using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.AzureServiceBus.Internal;

namespace Enable.Extensions.Queuing.AzureServiceBus
{
    public class AzureServiceBusQueueClientFactory : IQueueClientFactory
    {
        private readonly AzureServiceBusQueueClientFactoryOptions _options;
        private readonly int _prefetchCount;

        public AzureServiceBusQueueClientFactory(AzureServiceBusQueueClientFactoryOptions options)
            : this(options, 0)
        {
        }

        public AzureServiceBusQueueClientFactory(AzureServiceBusQueueClientFactoryOptions options, int prefetchCount)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new ArgumentNullException(nameof(options.ConnectionString));
            }

            _options = options;
            _prefetchCount = prefetchCount;
        }

        public IQueueClient GetQueueReference(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new AzureServiceBusQueueClient(
                _options.ConnectionString,
                queueName,
                _prefetchCount);
        }
    }
}
