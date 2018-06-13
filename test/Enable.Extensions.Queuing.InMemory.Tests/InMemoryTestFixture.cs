using System;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.InMemory.Tests
{
    public class InMemoryTestFixture
    {
        public string QueueName { get; } = Guid.NewGuid().ToString();

        public async Task ClearQueue()
        {
            var queueFactory = new InMemoryQueueClientFactory();

            using (var queueClient = queueFactory.GetQueueReference(QueueName))
            {
                try
                {
                    IQueueMessage message;

                    while ((message = await queueClient.DequeueAsync()) != null)
                    {
                        await queueClient.CompleteAsync(message);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }
}
