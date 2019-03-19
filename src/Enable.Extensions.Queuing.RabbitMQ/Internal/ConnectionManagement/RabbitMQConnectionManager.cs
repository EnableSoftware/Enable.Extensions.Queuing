using System;
using System.Collections.Generic;
using System.Linq;
using Enable.Extensions.Queuing.RabbitMQ.Internal.ConnectionManagement;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQConnectionManager
    {
        private static readonly List<(int hash, RabbitMQManagedConnection managedConnection)>
            _connections = new List<(int hash, RabbitMQManagedConnection managedConnection)>();

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

        /// <summary>
        /// Provides a new RabbitMQ channel, session and model. The underlying connection is managed,
        /// ensuring a connection is reused if possible.
        /// </summary>
        public RabbitMQManagedChannel CreateChannel()
        {
            RabbitMQManagedConnection managedConnection = null;
            IModel channel = null;

            managedConnection = GetConnection();

            try
            {
                channel = managedConnection.Connection.CreateModel();
            }
            catch (Exception ex)
            {
                if (ex is ChannelAllocationException || ex is AlreadyClosedException)
                {
                    managedConnection.ChannelAllocationReached = true;

                    managedConnection = CreateConnection();
                    channel = managedConnection.Connection.CreateModel();
                }
                else
                {
                    throw;
                }
            }

            return new RabbitMQManagedChannel(channel, managedConnection.Id);
        }

        /// <summary>
        /// Closes and disposes the channel. The underlying connection will be released and closed if no other clients are using it.
        /// </summary>
        public void CloseChannel(RabbitMQManagedChannel managedChannel)
        {
            var channel = managedChannel.Channel;

            channel.Close();
            channel.Dispose();

            ReleaseConnection(managedChannel.ConnectionId);
        }

        /// <summary>
        /// Provides access to the underlying RabbitMQ connection. Clients need not be concerned of the state of the connections available.
        /// <see cref="ReleaseConnection"/> must be called when the connection is no longer required.
        /// </summary>
        private RabbitMQManagedConnection GetConnection()
        {
            lock (_lock)
            {
                var(hash, managedConnection) = _connections
                    .Find(c => c.hash == _connectionHash
                    && !c.managedConnection.ChannelAllocationReached);

                if (managedConnection == null || managedConnection?.Connection == null)
                {
                    return CreateConnection();
                }

                managedConnection.IncrementReferenceCount();

                return managedConnection;
            }
        }

        /// <summary>
        /// Tells the connection manager that the connection is no longer required. The RabbitMQ
        /// connection will be closed if no other clients are using it.
        /// </summary>
        private void ReleaseConnection(Guid connectionId)
        {
            lock (_lock)
            {
                var(hash, managedConnection) = _connections.Find(c => c.managedConnection.Id == connectionId);

                if (managedConnection != null || managedConnection?.Connection != null)
                {
                    managedConnection.DecrementReferenceCount();

                    if (managedConnection.ReferenceCount == 0)
                    {
                        managedConnection.Dispose();

                        _connections.Remove((hash, managedConnection));
                    }
                }
            }
        }

        /// <summary>
        /// Explicity creates a new RabbitMQ connection.
        /// </summary>
        private RabbitMQManagedConnection CreateConnection()
        {
            lock (_lock)
            {
                var connection = _connectionFactory.CreateConnection();
                var managedConnection = new RabbitMQManagedConnection(connection);

                _connections.Add((_connectionHash, managedConnection));

                return managedConnection;
            }
        }
    }
}
