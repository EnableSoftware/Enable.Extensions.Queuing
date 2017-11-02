using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.InMemory.Internal;

namespace Enable.Extensions.Queuing.InMemory
{
    public class InMemoryQueueClientFactory : IQueueClientFactory
    {
        public IQueueClient GetQueueReference(string queueName)
        {
            return new InMemoryQueueClient(queueName);
        }
    }
}
