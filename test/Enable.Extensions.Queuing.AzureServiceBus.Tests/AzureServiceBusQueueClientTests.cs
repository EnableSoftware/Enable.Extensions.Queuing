using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Xunit;

namespace Enable.Extensions.Queuing.AzureServiceBus.Tests
{
    public class AzureServiceBusQueueClientTests : IClassFixture<AzureServiceBusTestFixture>, IDisposable
    {
        private readonly AzureServiceBusTestFixture _fixture;

        private readonly IQueueClient _sut;

        private bool _disposed;

        public AzureServiceBusQueueClientTests(AzureServiceBusTestFixture fixture)
        {
            var options = new AzureServiceBusQueueClientFactoryOptions
            {
                ConnectionString = fixture.ConnectionString
            };

            var queueFactory = new AzureServiceBusQueueClientFactory(options);

            _sut = queueFactory.GetQueueReference(fixture.QueueName);

            fixture.ClearQueue().GetAwaiter().GetResult();

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
            await _sut.RenewLockAsync(message, CancellationToken.None);
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
                        _fixture.ClearQueue()
                            .GetAwaiter()
                            .GetResult();
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
