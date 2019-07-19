using System;

namespace Enable.Extensions.Queuing.AzureServiceBus
{
    public class AzureServiceBusQueueClientFactoryOptions
    {
        private int _prefetchCount;

        /// <summary>
        /// Gets or sets the connection string for the Azure Service Bus resource
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether queues will be disposed when they are no longer referenced.
        /// </summary>
        public bool DisposeQueueWhenNotInUse { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how many messages should be pre-fetched.
        /// IMPORTANT: Prefetched messages don't auto renew their lock, if PrefetchCount is set to a high value, messages may be received more than once.
        /// </summary>
        public int PrefetchCount
        {
            get
            {
                return _prefetchCount;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        $"The specified value '{value}' is invalid. '{nameof(PrefetchCount)}' must be greater than or equal to zero.");
                }

                _prefetchCount = value;
            }
        }
    }
}
