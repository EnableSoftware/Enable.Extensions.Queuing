using System;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Tests
{
    public class RabbitMQTestFixture : IDisposable
    {
        private readonly ConnectionFactory _connectionFactory;

        private bool _disposed;

        public RabbitMQTestFixture()
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = HostName,
                Port = Port,
                VirtualHost = VirtualHost,
                UserName = UserName,
                Password = Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
        }

        public string HostName { get; } = "localhost";

        public int Port { get; } = 5672;

        public string VirtualHost { get; } = ConnectionFactory.DefaultVHost;

        public string UserName { get; } = ConnectionFactory.DefaultUser;

        public string Password { get; } = ConnectionFactory.DefaultPass;

        public string QueueName { get; } = Guid.NewGuid().ToString();

        public void ClearQueue()
        {
            using (var connection = _connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueuePurge(QueueName);
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
                    try
                    {
                        // Make a best effort to remove our temporary test queue.
                        DeleteQueue();
                    }
                    catch
                    {
                    }
                }

                _disposed = true;
            }
        }

        private void DeleteQueue()
        {
            using (var connection = _connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDelete(QueueName, ifUnused: false, ifEmpty: false);
            }
        }
    }
}
