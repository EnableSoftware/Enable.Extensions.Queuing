using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.InMemory.Internal;

namespace Enable.Extensions.Queuing.InMemory
{
    public class InMemoryQueueClientFactory : IQueueClientFactory
    {
        public IQueueClient GetQueueReference(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new InMemoryQueueClient(queueName);
        }
    }
}
