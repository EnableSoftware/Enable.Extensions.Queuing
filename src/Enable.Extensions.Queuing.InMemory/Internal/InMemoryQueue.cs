using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using InMemoryQueue = System.Collections.Concurrent.ConcurrentQueue<Enable.Extensions.Queuing.Abstractions.IQueueMessage>;

namespace Enable.Extensions.Queuing.InMemory.Internal
{
    internal class InMemoryQueue
    {
        private readonly ConcurrentQueue<IQueueMessage> _queue = new ConcurrentQueue<IQueueMessage>();

        private int _referenceCount = 0;

        public bool TryDequeue(out IQueueMessage message)
        {
            return _queue.TryDequeue(out message);
        }

        public void Enqueue(IQueueMessage message)
        {
            _queue.Enqueue(message);
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public int IncrementReferenceCount()
        {
            return Interlocked.Increment(ref _referenceCount);
        }

        public int DecrementReferenceCount()
        {
            return Interlocked.Decrement(ref _referenceCount);
        }

    }
}
