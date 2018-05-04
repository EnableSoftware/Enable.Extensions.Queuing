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
        private readonly InMemoryQueueClient _queueClient;
        private int _referenceCount = 0;

        private Func<IQueueMessage, CancellationToken, Task> _messageHandler;

        public InMemoryQueue(InMemoryQueueClient queueClient)
        {
            _queueClient = queueClient ?? throw new ArgumentNullException(nameof(queueClient));
        }

        public bool TryDequeue(out IQueueMessage message)
        {
            return _queue.TryDequeue(out message);
        }

        public void Enqueue(IQueueMessage message)
        {
            _queue.Enqueue(message);

            try
            {
                _messageHandler?.Invoke(message, CancellationToken.None);
                Task.Run(async () => await _queueClient.CompleteAsync(message)).Wait();
            }
            catch
            {
                Task.Run(async () => await _queueClient.AbandonAsync(message)).Wait();
                throw;
            }
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

        public void RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> handler)
        {
            if (_messageHandler != null)
            {
                throw new InvalidOperationException("A message handler has already been registered.");
            }

            _messageHandler = handler;
        }
    }
}
