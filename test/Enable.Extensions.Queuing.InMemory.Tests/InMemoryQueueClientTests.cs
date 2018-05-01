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
        public async Task RegisterMessageHandler_CanInvoke()
        {
            // Act
            await _sut.RegisterMessageHandler(
                (message, cancellationToken) => throw new Exception("There should be no messages to process."));
        }

        [Fact]
        public async Task RegisterMessageHandler_MessageHandlerInvoked()
        {
            // Arrange
            var messageHandled = false;

            Func<IQueueMessage, CancellationToken, Task> handler
                = (message, cancellationToken) =>
                {
                    messageHandled = true;
                    return Task.CompletedTask;
                };

            await _sut.RegisterMessageHandler(handler);

            // Act
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            // Assert
            Assert.True(messageHandled);
        }

        [Fact]
        public async Task RegisterMessageHandler_MessageHandlerInvokedAcrossInstances()
        {
            // Arrange
            var queueName = Guid.NewGuid().ToString();

            var queueFactory = new InMemoryQueueClientFactory();

            using (var instance1 = queueFactory.GetQueueReference(queueName))
            using (var instance2 = queueFactory.GetQueueReference(queueName))
            {
                var content = Guid.NewGuid().ToString();

                var messageHandled = false;

               Func<IQueueMessage, CancellationToken, Task> handler
                    = (message, cancellationToken) =>
                    {
                        messageHandled = true;
                        return Task.CompletedTask;
                    };

                await instance2.RegisterMessageHandler(handler);

                // Act
                await instance1.EnqueueAsync(content, CancellationToken.None);

                // Assert
                Assert.True(messageHandled);
            }
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

        [Fact]
        public async Task CanDequeueAcrossInstances()
        {
            // Arrange
            var queueName = Guid.NewGuid().ToString();

            var queueFactory = new InMemoryQueueClientFactory();

            using (var instance1 = queueFactory.GetQueueReference(queueName))
            using (var instance2 = queueFactory.GetQueueReference(queueName))
            {
                var content = Guid.NewGuid().ToString();

                await instance1.EnqueueAsync(content, CancellationToken.None);

                // Act
                var message = await instance2.DequeueAsync(CancellationToken.None);

                // Assert
                Assert.Equal(content, message.GetBody<string>());
            }
        }

        [Fact]
        public async Task CanDequeueAcrossInstances_WithDisposedInstance()
        {
            // Arrange
            var queueName = Guid.NewGuid().ToString();

            var queueFactory = new InMemoryQueueClientFactory();

            var content = Guid.NewGuid().ToString();

            using (var instance1 = queueFactory.GetQueueReference(queueName))
            {
                await instance1.EnqueueAsync(content, CancellationToken.None);
            }

            var instance2 = queueFactory.GetQueueReference(queueName);

            // Act
            var message = await instance2.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Equal(content, message.GetBody<string>());
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
