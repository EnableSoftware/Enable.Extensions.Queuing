using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using InMemoryQueue = System.Collections.Concurrent.ConcurrentQueue<Enable.Extensions.Queuing.Abstractions.IQueueMessage>;

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

        private readonly string _queueName;

        private bool _disposed;

        public InMemoryQueueClient(string queueName)
        {
            var queue = new ConcurrentQueue<IQueueMessage>();

            _queues.TryAdd(queueName, queue);

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

            if (_queues.TryGetValue(_queueName, out InMemoryQueue queue))
            {
                if (queue.TryDequeue(out IQueueMessage message))
                {
                    return Task.FromResult(message);
                }

                return Task.FromResult<IQueueMessage>(null);
            }

            throw new NotSupportedException();
        }

        public override Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            if (_queues.TryGetValue(_queueName, out InMemoryQueue queue))
            {
                queue.Enqueue(message);

                return Task.CompletedTask;
            }

            throw new NotSupportedException();
        }

        public override Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _queues.Clear();
                }

                _disposed = true;
            }
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
