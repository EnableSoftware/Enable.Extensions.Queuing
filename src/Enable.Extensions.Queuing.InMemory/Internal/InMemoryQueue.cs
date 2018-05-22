using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.InMemory.Internal
{
    internal class InMemoryQueue
    {
        private readonly ConcurrentQueue<IQueueMessage> _queue = new ConcurrentQueue<IQueueMessage>();

        private int _referenceCount = 0;

        private Func<IQueueMessage, CancellationToken, Task> _messageHandler;

        public bool TryDequeue(out IQueueMessage message)
        {
            return _queue.TryDequeue(out message);
        }

        public void Enqueue(IQueueMessage message)
        {
            // If a message handler has been registered, we attempt to
            // invoke the handler. If this successfully processes a
            // message then no further work is required.
            if (!TryInvokeMessageHandler(message))
            {
                // If there is no message handler registered, or if this
                // throws an exception, then we simply queue the message.
                _queue.Enqueue(message);
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

        private bool TryInvokeMessageHandler(IQueueMessage message)
        {
            try
            {
                if (_messageHandler != null)
                {
                    _messageHandler.Invoke(message, CancellationToken.None);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
