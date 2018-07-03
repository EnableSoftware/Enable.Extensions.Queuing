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

            _fixture = fixture;
        }

        [Fact]
        public async Task EnqueueAsync_CanInvoke()
        {
            // Arrange
            var content = Guid.NewGuid().ToString();

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Clean up
            var message = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message, CancellationToken.None);
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
            var evt = new ManualResetEvent(false);

            Task MessageHandler(IQueueMessage message, CancellationToken cancellationToken)
            {
                evt.Set();
                return Task.CompletedTask;
            }

            await _sut.RegisterMessageHandler(MessageHandler);

            // Act
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            // Assert
            Assert.True(evt.WaitOne(TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public async Task RegisterMessageHandler_ThrowsOnMutipleMessageHandlerRegistrations()
        {
            // Arrange
            Task MessageHandler(IQueueMessage message, CancellationToken cancellationToken)
            {
                throw new Exception("There should be no messages to process.");
            }

            await _sut.RegisterMessageHandler(MessageHandler);

            // Act
            var exception = await Record.ExceptionAsync(() => _sut.RegisterMessageHandler(MessageHandler));

            // Assert
            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public async Task RegisterMessageHandler_ThrowsForNullMessageHandlerOptions()
        {
            // Arrange
            Task MessageHandler(IQueueMessage message, CancellationToken cancellationToken)
            {
                throw new Exception("There should be no messages to process.");
            }

            // Act
            var exception = await Record.ExceptionAsync(
                () => _sut.RegisterMessageHandler(MessageHandler, null));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task RegisterMessageHandler_CanSetMessageHandlerOptions()
        {
            // Arrange
            Task MessageHandler(IQueueMessage message, CancellationToken cancellationToken)
            {
                throw new Exception("There should be no messages to process.");
            }

            var options = new MessageHandlerOptions
            {
                MaxConcurrentCalls = 1,
                ExceptionReceivedHandler = (_) => Task.CompletedTask
            };

            // Act
            await _sut.RegisterMessageHandler(MessageHandler, options);
        }

        [Fact]
        public async Task RegisterMessageHandler_ExceptionHandlerInvoked()
        {
            // Arrange
            var evt = new ManualResetEvent(false);

            Task MessageHandler(IQueueMessage message, CancellationToken cancellationToken)
            {
                throw new Exception("Message failed processing.");
            }

            Task ExceptionHandler(MessageHandlerExceptionContext context)
            {
                evt.Set();
                return Task.CompletedTask;
            }

            var options = new MessageHandlerOptions
            {
                MaxConcurrentCalls = 1,
                ExceptionReceivedHandler = ExceptionHandler
            };

            await _sut.RegisterMessageHandler(MessageHandler, options);

            // Act
            await _sut.EnqueueAsync(
                Guid.NewGuid().ToString(),
                CancellationToken.None);

            // Assert
            Assert.True(evt.WaitOne(TimeSpan.FromSeconds(1)));
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
                }

                _disposed = true;
            }
        }
    }
}
