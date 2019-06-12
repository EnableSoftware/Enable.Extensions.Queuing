using System;

namespace Enable.Extensions.Queuing.AzureServiceBus
{
    public class AzureServiceBusQueueClientOptions
    {
        private int _prefetchCount;

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
