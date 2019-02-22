using System;
using System.Linq;
using System.Net.NetworkInformation;
using Xunit;

namespace Enable.Extensions.Queuing.RabbitMQ.Tests
{
    public class RabbitMQQueueClientFactoryTests : IClassFixture<RabbitMQTestFixture>, IDisposable
    {
        private readonly RabbitMQTestFixture _fixture;

        private readonly RabbitMQQueueClientFactoryOptions _options;

        private bool _disposed;

        public RabbitMQQueueClientFactoryTests(RabbitMQTestFixture fixture)
        {
            _options = new RabbitMQQueueClientFactoryOptions
            {
                HostName = fixture.HostName,
                Port = fixture.Port,
                VirtualHost = fixture.VirtualHost,
                UserName = fixture.UserName,
                Password = fixture.Password
            };

            _fixture = fixture;
        }

        [Fact]
        public void ShouldUse_OneConnection_MultipleRabbitMQClientsCreated()
        {
            // Arrange
            var sut = new RabbitMQQueueClientFactory(_options);

            var connectionsBeforeTest = OpenRabbitMQConnectionsCount();

            // Act
            sut.GetQueueReference(_fixture.QueueName);
            sut.GetQueueReference(_fixture.QueueName);

            var connectionsAfterTest = OpenRabbitMQConnectionsCount();

            Assert.Equal(connectionsBeforeTest + 1, connectionsAfterTest);
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
                        // Make a best effort to clear our test queue.
                        _fixture.ClearQueue();
                    }
                    catch
                    {
                    }
                }

                _disposed = true;
            }
        }

        private int OpenRabbitMQConnectionsCount()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var connections = properties.GetActiveTcpConnections();

            return connections.Count(c =>
            (c.State != TcpState.Closed
            || c.State != TcpState.Closing
            || c.State != TcpState.Closing)
            && c.RemoteEndPoint.Port == _fixture.Port);
        }
    }
}
