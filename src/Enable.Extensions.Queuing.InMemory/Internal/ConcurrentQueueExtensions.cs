using System.Collections.Concurrent;

namespace Enable.Extensions.Queuing.InMemory.Internal
{
    internal static class ConcurrentQueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            if (queue != null)
            {
                while (queue.TryDequeue(out T _))
                {
                    // Do nothing, since we're just trying to drain the queue.
                }
            }
        }
    }
}
