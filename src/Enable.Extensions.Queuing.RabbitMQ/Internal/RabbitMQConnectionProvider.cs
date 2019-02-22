using System;
using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQConnectionProvider : IDisposable
    {
        private static readonly ConcurrentDictionary<int, WeakReference<IConnection>>
            _connections = new ConcurrentDictionary<int, WeakReference<IConnection>>();

        private static readonly object _lock = new object();

        private readonly IConnectionFactory _connectionFactory;
        private readonly int _connectionHash;
        private IConnection _connection;
        private bool _disposed;

        public RabbitMQConnectionProvider(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connectionHash = new { _connectionFactory.VirtualHost, _connectionFactory.UserName, connectionFactory.Password }.GetHashCode();
        }

        public IConnection GetConnection()
        {
            lock (_lock)
            {
                if (_connection == null)
                {
                 _connections.TryGetValue(
                    _connectionHash,
                    out var weakConnection);

                    if (weakConnection == null)
                    {
                        weakConnection = new WeakReference<IConnection>(_connectionFactory.CreateConnection());
                        _connections.TryAdd(_connectionHash, weakConnection);
                    }

                    weakConnection.TryGetTarget(out _connection);
                }
            }

            return _connection;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _connections.TryGetValue(_connectionHash, out var weakConnection))
                {
                    weakConnection.TryGetTarget(out var connection);

                    if (connection == null)
                    {
                        _connections.TryRemove(_connectionHash, out _);
                    }
                }

                _disposed = true;
            }
        }
    }
}
