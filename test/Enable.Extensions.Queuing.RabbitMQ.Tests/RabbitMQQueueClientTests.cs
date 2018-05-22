using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Xunit;

namespace Enable.Extensions.Queuing.RabbitMQ.Tests
{
    public class RabbitMQQueueClientTests : IClassFixture<RabbitMQTestFixture>, IDisposable
    {
        private readonly RabbitMQTestFixture _fixture;

        private readonly IQueueClient _sut;

        private bool _disposed;

        public RabbitMQQueueClientTests(RabbitMQTestFixture fixture)
        {
            var options = new RabbitMQQueueClientFactoryOptions
            {
                HostName = fixture.HostName,
                Port = fixture.Port,
                VirtualHost = fixture.VirtualHost,
                UserName = fixture.UserName,
                Password = fixture.Password
            };

            var queueFactory = new RabbitMQQueueClientFactory(options);

            _sut = queueFactory.GetQueueReference(fixture.QueueName);

            fixture.ClearQueue();

            _fixture = fixture;
        }

        [Fact]
        public async Task EnqueueAsync_CanInvoke()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);
        }

        [Fact]
        public async Task DequeueAsync_CanInvoke()
        {
            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Null(message);
        }

        [Fact]
        public async Task DequeueAsync_ReturnsEnqueuedMessage()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(message);
        }

        [Fact]
        public async Task DequeueAsync_CanDeserializeMessage()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Equal(content, message.GetBody<string>());
        }

        [Fact]
        public async Task AbandonAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.AbandonAsync(message, CancellationToken.None);
        }

        [Fact]
        public async Task CompleteAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.CompleteAsync(message, CancellationToken.None);
        }

        [Fact]
        public async Task RenewLockAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            var exception = await Record.ExceptionAsync(() => _sut.RenewLockAsync(message, CancellationToken.None));

            // Assert
            Assert.IsType<NotImplementedException>(exception);
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

                    _sut.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
