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
            _fixture = fixture;

            var options = new AzureServiceBusQueueClientFactoryOptions
            {
                ConnectionString = _fixture.ConnectionString
            };

            var queueFactory = new AzureServiceBusQueueClientFactory(options);

            var queueName = _fixture.QueueName;

            _sut = queueFactory.GetQueueReference(queueName);
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

            // Clean up
            await _sut.CompleteAsync(message, CancellationToken.None);
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

            // Clean up
            await _sut.CompleteAsync(message, CancellationToken.None);
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

            // Clean up
            message = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message, CancellationToken.None);
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
        public async Task RenewLockAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.RenewLockAsync(message, CancellationToken.None);

            // Clean up
            await _sut.CompleteAsync(message, CancellationToken.None);
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
