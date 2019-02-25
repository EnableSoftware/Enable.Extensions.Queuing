using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQConnectionFactory : IDisposable
    {
        private static readonly Dictionary<int, WeakReference<IConnection>>
            _connections = new Dictionary<int, WeakReference<IConnection>>();

        private static readonly object _lock = new object();

        private readonly int _connectionHash;
        private readonly IConnectionFactory _connectionFactory;
        private readonly RabbitMQQueueClientFactoryOptions _options;

        private bool _disposed;

        public RabbitMQConnectionFactory(RabbitMQQueueClientFactoryOptions options)
        {
            _options = options;

            _connectionFactory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,
                AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = options.NetworkRecoveryInterval
            };

            _connectionHash = new
            {
                _options.HostName,
                _options.Port,
                _options.VirtualHost,
                _options.UserName,
                _options.Password,
                _options.AutomaticRecoveryEnabled,
                _options.NetworkRecoveryInterval,
            }.GetHashCode();
        }

        public IConnection GetOrCreateConnection()
        {
            lock (_lock)
            {
                IConnection connection;

                _connections.TryGetValue(_connectionHash, out var weakConnection);

                if (weakConnection != null)
                {
                    weakConnection.TryGetTarget(out connection);

                    if (connection == null)
                    {
                        connection = _connectionFactory.CreateConnection();
                        weakConnection.SetTarget(connection);
                    }
                }
                else
                {
                    connection = CreateConnection();
                }

                return connection;
            }
        }

        public IConnection CreateConnection()
        {
            lock (_lock)
            {
                var connection = _connectionFactory.CreateConnection();
                var weakConnection = new WeakReference<IConnection>(connection);

                AddOrUpdateConnection(weakConnection);

                return connection;
            }
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
                    if (UnusedConnectionExists(_connectionHash))
                    {
                        _connections.Remove(_connectionHash);
                    }
                }

                _disposed = true;
            }
        }

        private void AddOrUpdateConnection(WeakReference<IConnection> weakConnection)
        {
            if (_connections.ContainsKey(_connectionHash))
            {
                _connections[_connectionHash] = weakConnection;
            }
            else
            {
                _connections.Add(_connectionHash, weakConnection);
            }
        }

        private bool UnusedConnectionExists(int connectionHash)
        {
            return _connections.TryGetValue(connectionHash, out var weakConnection) && !weakConnection.TryGetTarget(out var connection);
        }
    }
}
