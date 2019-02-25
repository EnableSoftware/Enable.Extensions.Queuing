using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQConnectionManager
    {
        private static readonly Dictionary<int, RabbitMQManagedConnection>
            _connections = new Dictionary<int, RabbitMQManagedConnection>();

        private static readonly object _lock = new object();

        private readonly int _connectionHash;
        private readonly IConnectionFactory _connectionFactory;
        private readonly RabbitMQQueueClientFactoryOptions _options;

        public RabbitMQConnectionManager(RabbitMQQueueClientFactoryOptions options)
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
                _connections.TryGetValue(_connectionHash, out var managedConnection);

                if (managedConnection == null || managedConnection.Disposed)
                {
                    return CreateConnection();
                }
                else
                {
                    managedConnection.IncrementReferenceCount();

                    return managedConnection.Connection;
                }
            }
        }

        public IConnection CreateConnection()
        {
            lock (_lock)
            {
                var connection = _connectionFactory.CreateConnection();
                var managedConnection = new RabbitMQManagedConnection(connection);

                AddOrUpdateConnection(managedConnection);

                return connection;
            }
        }

        public void ReleaseConnection()
        {
            lock (_lock)
            {
                if (_connections.TryGetValue(_connectionHash, out var managedConnection))
                {
                    managedConnection.DecrementReferenceCount();

                    if (managedConnection.ReferenceCount == 0)
                    {
                        managedConnection.Dispose();

                        _connections.Remove(_connectionHash);
                    }
                }
            }
        }

        private void AddOrUpdateConnection(RabbitMQManagedConnection managedConnection)
        {
            if (_connections.ContainsKey(_connectionHash))
            {
                _connections[_connectionHash] = managedConnection;
            }
            else
            {
                _connections.Add(_connectionHash, managedConnection);
            }
        }
    }
}
