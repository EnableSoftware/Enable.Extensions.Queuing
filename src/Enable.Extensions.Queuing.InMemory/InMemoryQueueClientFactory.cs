using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.InMemory.Internal;

namespace Enable.Extensions.Queuing.InMemory
{
    public class InMemoryQueueClientFactory : IQueueClientFactory
    {
        public IQueueClient GetQueueReference(string queueName, string deadLetterQueueName = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            // We do not currently have a dead letter queue for the in-memory client, so we do not pass the name.
            return new InMemoryQueueClient(queueName);
        }
    }
}
