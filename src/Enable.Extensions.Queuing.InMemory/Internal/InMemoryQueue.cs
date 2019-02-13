using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.InMemory.Internal
{
    internal class InMemoryQueue
    {
        private readonly ConcurrentQueue<IQueueMessage> _queue = new ConcurrentQueue<IQueueMessage>();

        private int _referenceCount = 1;

        private Func<IQueueMessage, CancellationToken, Task> _messageHandler;

        private MessageHandlerOptions _messageHandlerOptions;

        public void Clear()
        {
            _queue.Clear();
        }

        public void ClearRegisteredMessageHandler()
        {
            _messageHandler = null;
            _messageHandlerOptions = null;
        }

        public int DecrementReferenceCount()
        {
            return Interlocked.Decrement(ref _referenceCount);
        }

        public Task<IQueueMessage> DequeueAsync()
        {
            if (_queue.TryDequeue(out IQueueMessage message))
            {
                return Task.FromResult(message);
            }

            return Task.FromResult<IQueueMessage>(null);
        }

        public async Task EnqueueAsync(IQueueMessage message)
        {
            // If a message handler has been registered, we attempt to
            // invoke the handler. If this successfully processes a
            // message then no further work is required.
            var messageHandled = await TryInvokeMessageHandlerAsync(message);

            if (!messageHandled)
            {
                // If there is no message handler registered, or if this
                // throws an exception, then we simply queue the message.
                _queue.Enqueue(message);
            }
        }

        public Task EnqueueAsync(IEnumerable<IQueueMessage> messages)
        {
            var tasks = new List<Task>();

            foreach (var message in messages)
            {
                tasks.Add(EnqueueAsync(message));
            }

            return Task.WhenAll(tasks);
        }

        public int IncrementReferenceCount()
        {
            return Interlocked.Increment(ref _referenceCount);
        }

        public void RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> messageHandler,
            MessageHandlerOptions messageHandlerOptions)
        {
            if (_messageHandler != null)
            {
                throw new InvalidOperationException("A message handler has already been registered.");
            }

            _messageHandler = messageHandler;
            _messageHandlerOptions = messageHandlerOptions;
        }

        private async Task<bool> TryInvokeMessageHandlerAsync(IQueueMessage message)
        {
            try
            {
                if (_messageHandler != null)
                {
                    await _messageHandler(message, CancellationToken.None);
                    return true;
                }
            }
            catch (Exception ex)
            {
                var exceptionReceivedHandler = _messageHandlerOptions?.ExceptionReceivedHandler;

                if (exceptionReceivedHandler != null)
                {
                    var context = new MessageHandlerExceptionContext(ex);

                    await exceptionReceivedHandler(context);
                }
            }

            return false;
        }
    }
}
