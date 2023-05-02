using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.AzureServiceBus.Internal;

namespace Enable.Extensions.Queuing.AzureServiceBus
{
    public class AzureServiceBusQueueClientFactory : IQueueClientFactory
    {
        private readonly AzureServiceBusQueueClientFactoryOptions _options;

        public AzureServiceBusQueueClientFactory(AzureServiceBusQueueClientFactoryOptions options)
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
        }

        public IQueueClient GetQueueReference(string queueName, QueueOptions queueOptions = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            // We do not currently define a dead letter queue in service bus client, so we do not pass the queueOptions.
            return new AzureServiceBusQueueClient(
                _options.ConnectionString,
                queueName,
                _options.PrefetchCount,
                _options.DisposeQueueWhenNotInUse);
        }
    }
}
