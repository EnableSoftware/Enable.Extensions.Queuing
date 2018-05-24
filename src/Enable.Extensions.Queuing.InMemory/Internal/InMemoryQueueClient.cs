using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.InMemory.Internal
{
    /// <summary>
    /// Represents an in memory messaging queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This queue implementation is useful when you want to test components
    /// using something that approximates connecting to a real queue, without
    /// the overhead of actual queue operations. 
    /// </para>
    /// <para>
    /// This queue implementation is intended to be used in test code only.
    /// This queue is only valid for the lifetime of the host process and
    /// cannot be used for inter-process communication. 
    /// </para>
    /// </remarks>
    internal class InMemoryQueueClient : BaseQueueClient
    {
        private readonly static ConcurrentDictionary<string, InMemoryQueue> _queues =
            new ConcurrentDictionary<string, InMemoryQueue>();

        private readonly InMemoryQueue _queue;

        private readonly string _queueName;

        private bool _disposed;

        public InMemoryQueueClient(string queueName)
        {
            // Here we add a new queue instance to a cache of queues if we've
            // not seen this value of `queueName` before, or we increment a
            // reference count on the previously cached queue. This reference
            // counter is used to clear the queue only once all references to
            // queues for the same `queueName` are disposed.
            _queue = _queues.AddOrUpdate(
                queueName,
                new InMemoryQueue(),
                (key, oldValue) =>
                {
                    oldValue.IncrementReferenceCount();
                    return oldValue;
                });

            _queueName = queueName;
        }

        public override Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return Task.CompletedTask;
        }

        public override Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return Task.CompletedTask;
        }

        public override Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return _queue.DequeueAsync();
        }

        public override Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return _queue.EnqueueAsync(message);
        }

        public override Task RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> messageHandler,
            MessageHandlerOptions messageHandlerOptions)
        {
            ThrowIfDisposed();

            _queue.RegisterMessageHandler(messageHandler, messageHandlerOptions);

            return Task.CompletedTask;
        }

        public override Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    var referenceCount = _queue.DecrementReferenceCount();

                    if (referenceCount == 0)
                    {
                        // Clear and remove the queue if there are no longer
                        // any references to it.
                        _queue.Clear();
                        _queues.TryRemove(_queueName, out _);
                    }
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
