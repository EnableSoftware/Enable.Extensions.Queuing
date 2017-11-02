using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Xunit;

namespace Enable.Extensions.Queuing.InMemory.Tests
{
    public class InMemoryQueueClientTests : IDisposable
    {
        private readonly IQueueClient _sut;

        private bool _disposed;

        public InMemoryQueueClientTests()
        {
            var queueFactory = new InMemoryQueueClientFactory();

            var queueName = Guid.NewGuid().ToString();

            _sut = queueFactory.GetQueueReference(queueName);
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
            // Arrange
            var content = Guid.NewGuid().ToString();

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
                    _sut.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
