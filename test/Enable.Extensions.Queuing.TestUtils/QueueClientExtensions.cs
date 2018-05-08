using System;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.TestUtils
{
    public static class QueueClientExtensions
    {
        public static async Task Clear(this IQueueClient queueClient)
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
