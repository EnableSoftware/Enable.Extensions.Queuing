using System;
using System.Collections.Concurrent;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.Discovery
{
    public class QueueManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, IQueueClient> _queueClients =
            new ConcurrentDictionary<string, IQueueClient>(StringComparer.OrdinalIgnoreCase);

        private readonly IQueueClientFactory _queueClientFactory;

        private bool _disposed;

        public QueueManager(IQueueClientFactory queueClientFactory)
        {
            _queueClientFactory = queueClientFactory ?? throw new ArgumentNullException(nameof(queueClientFactory));
        }

        public IQueueClient GetOrAddQueueClient(string entityPath)
        {
            if (_disposed)
            {
                throw new InvalidOperationException();
            }

            return _queueClients.GetOrAdd(
                entityPath,
                o => _queueClientFactory.GetQueueReference(entityPath));
        }

        public void Dispose()
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
                    foreach (var queueName in _queueClients.Keys)
                    {
                        if (_queueClients.TryRemove(queueName, out var queueClient))
                        {
                            queueClient.Dispose();
                        }
                    }
                }

                _disposed = true;
            }
        }
    }
}
