using System;
using System.Collections.Generic;
using System.Text;
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
        public async Task EnqueueAsync_CanInvokeWithString()
        {
            // Arrange
            var content = _fixture.CreateMessage();

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Clean up
            var message = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message, CancellationToken.None);
        }

        [Fact]
        public async Task EnqueueAsync_CanInvokeWithByteArray()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes(_fixture.CreateMessage());

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Clean up
            var message = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message, CancellationToken.None);
        }

        [Fact]
        public async Task EnqueueAsync_CanInvokeWithCustomMessageType()
        {
            // Arrange
            var content = new CustomMessageType
            {
                Message = _fixture.CreateMessage()
            };

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Clean up
            var message = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message, CancellationToken.None);
        }

        [Fact]
        public async Task EnqueueAsync_CanInvokeWithMultipleMessages()
        {
            // Arrange
            var messages = new List<string>
            {
                _fixture.CreateMessage(),
                _fixture.CreateMessage()
            };

            // Act
            await _sut.EnqueueAsync<string>(messages, CancellationToken.None);

            // Clean up
            var message1 = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message1, CancellationToken.None);
            var message2 = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message2, CancellationToken.None);
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
            var content = _fixture.CreateMessage();

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
            Console.WriteLine("{0}, Starting method: DequeueAsync_CanDeserializeMessage", DateTime.Now);

            // Arrange
            var content = _fixture.CreateMessage();

            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Equal(content, message.GetBody<string>());

            // Clean up
            await _sut.CompleteAsync(message, CancellationToken.None);
            Console.WriteLine("{0}, Finishing method: DequeueAsync_CanDeserializeMessage", DateTime.Now);
        }

        [Fact]
        public async Task AbandonAsync_CanInvoke()
        {
            Console.WriteLine("{0}, Starting method: AbandonAsync_CanInvoke", DateTime.Now);
            // Arrange
            await _sut.EnqueueAsync(
                _fixture.CreateMessage(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.AbandonAsync(message, CancellationToken.None);

            Console.WriteLine("{0}, Finishing method: AbandonAsync_CanInvoke", DateTime.Now);
        }

        [Fact]
        public async Task CompleteAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                _fixture.CreateMessage(),
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
            Console.WriteLine("{0}, Starting method: RegisterMessageHandler_MessageHandlerInvoked", DateTime.Now);
            var evt = new ManualResetEvent(false);

            Task MessageHandler(IQueueMessage message, CancellationToken cancellationToken)
            {
                evt.Set();
                return Task.CompletedTask;
            }

            await _sut.RegisterMessageHandler(MessageHandler);

            // Act
            await _sut.EnqueueAsync(
                _fixture.CreateMessage(),
                CancellationToken.None);

            // Assert
            Assert.True(evt.WaitOne(TimeSpan.FromSeconds(100)));

            // Clean up
            // Delay current thread to give service bus time to complete the dequeued message before the sut is disposed.
            // This ensures the message is not on the queue when another test starts.
            await Task.Delay(TimeSpan.FromSeconds(5));
            Console.WriteLine("{0}, Finishing method: RegisterMessageHandler_MessageHandlerInvoked", DateTime.Now);
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
                _fixture.CreateMessage(),
                CancellationToken.None);

            // Assert
            Assert.True(evt.WaitOne(TimeSpan.FromSeconds(100)));

            // Clean up
            // Delay current thread to give service bus time to abandon the message before the sut is disposed.
            // This ensures the message is not on the queue when another test starts.
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task RenewLockAsync_CanInvoke()
        {
            // Arrange
            await _sut.EnqueueAsync(
                _fixture.CreateMessage(),
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
                    Console.WriteLine("{0} Disposing", DateTime.Now);
                    _sut.Dispose();

                    try
                    {
                        Console.WriteLine("{0} Clearing queue", DateTime.Now);

                        // Make a best effort to clear our test queue.
                        _fixture.ClearQueue()
                            .GetAwaiter()
                            .GetResult();

                        Console.WriteLine("{0} Cleared queue", DateTime.Now);
                    }
                    catch
                    {
                    }
                }

                _disposed = true;
            }
        }

        private class CustomMessageType
        {
            public string Message { get; set; }
        }
    }
}
