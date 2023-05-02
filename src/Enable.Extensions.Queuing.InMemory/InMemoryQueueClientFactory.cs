using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.InMemory.Internal;

namespace Enable.Extensions.Queuing.InMemory
{
    public class InMemoryQueueClientFactory : IQueueClientFactory
    {
        public IQueueClient GetQueueReference(string queueName, QueueOptions queueOptions = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            // We do not currently have a dead letter queue for the in-memory client, so we do not pass the queueOptions.
            return new InMemoryQueueClient(queueName);
        }
    }
}
