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
            Console.WriteLine("{0} Starting method: EnqueueAsync_CanInvokeWithString", DateTime.Now);

            // Arrange
            var content = _fixture.CreateMessage();

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Clean up
            var message = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message, CancellationToken.None);

            Console.WriteLine("{0} Finishing method: EnqueueAsync_CanInvokeWithString", DateTime.Now);
        }

        [Fact]
        public async Task EnqueueAsync_CanInvokeWithByteArray()
        {
            Console.WriteLine("{0} Starting method: EnqueueAsync_CanInvokeWithByteArray", DateTime.Now);
            // Arrange
            var content = Encoding.UTF8.GetBytes(_fixture.CreateMessage());

            // Act
            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Clean up
            var message = await _sut.DequeueAsync(CancellationToken.None);
            await _sut.CompleteAsync(message, CancellationToken.None);

            Console.WriteLine("{0} Finishing method: EnqueueAsync_CanInvokeWithByteArray", DateTime.Now);
        }

        [Fact]
        public async Task EnqueueAsync_CanInvokeWithCustomMessageType()
        {
            Console.WriteLine("{0} Starting method: EnqueueAsync_CanInvokeWithCustomMessageType", DateTime.Now);

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

            Console.WriteLine("{0} Finishing method: EnqueueAsync_CanInvokeWithCustomMessageType", DateTime.Now);
        }

        [Fact]
        public async Task EnqueueAsync_CanInvokeWithMultipleMessages()
        {
            Console.WriteLine("{0} Starting method: EnqueueAsync_CanInvokeWithMultipleMessages", DateTime.Now);

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

            Console.WriteLine("{0} Finish method: EnqueueAsync_CanInvokeWithMultipleMessages", DateTime.Now);
        }

        [Fact]
        public async Task DequeueAsync_CanInvoke()
        {
            Console.WriteLine("{0} Starting method: DequeueAsync_CanInvoke", DateTime.Now);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Null(message);

            Console.WriteLine("{0} Finishing method: DequeueAsync_CanInvoke", DateTime.Now);
        }

        [Fact]
        public async Task DequeueAsync_ReturnsEnqueuedMessage()
        {
            Console.WriteLine("{0} Starting method: DequeueAsync_ReturnsEnqueuedMessage", DateTime.Now);

            // Arrange
            var content = _fixture.CreateMessage();

            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(message);

            // Clean up
            await _sut.CompleteAsync(message, CancellationToken.None);

            Console.WriteLine("{0} Finishing method: DequeueAsync_ReturnsEnqueuedMessage", DateTime.Now);
        }

        [Fact]
        public async Task DequeueAsync_CanDeserializeMessage()
        {
            Console.WriteLine("{0} Starting method: DequeueAsync_CanDeserializeMessage", DateTime.Now);

            // Arrange
            var content = _fixture.CreateMessage();

            await _sut.EnqueueAsync(content, CancellationToken.None);

            // Act
            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Assert
            Assert.Equal(content, message.GetBody<string>());

            // Clean up
            await _sut.CompleteAsync(message, CancellationToken.None);

            Console.WriteLine("{0} Finishing method: DequeueAsync_CanDeserializeMessage", DateTime.Now);
        }

        [Fact]
        public async Task AbandonAsync_CanInvoke()
        {
            Console.WriteLine("{0} Starting method: AbandonAsync_CanInvoke", DateTime.Now);

            // Arrange
            await _sut.EnqueueAsync(
                _fixture.CreateMessage(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.AbandonAsync(message, CancellationToken.None);

            Console.WriteLine("{0} Finishing method: AbandonAsync_CanInvoke", DateTime.Now);
        }

        [Fact]
        public async Task CompleteAsync_CanInvoke()
        {
            Console.WriteLine("{0} Starting method: CompleteAsync_CanInvoke", DateTime.Now);

            // Arrange
            await _sut.EnqueueAsync(
                _fixture.CreateMessage(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.CompleteAsync(message, CancellationToken.None);

            Console.WriteLine("{0} Finishing method: CompleteAsync_CanInvoke", DateTime.Now);
        }

        [Fact]
        public async Task RegisterMessageHandler_CanInvoke()
        {
            Console.WriteLine("{0} Starting method: RegisterMessageHandler_CanInvoke", DateTime.Now);

            // Act
            await _sut.RegisterMessageHandler(
                (message, cancellationToken) => throw new Exception("There should be no messages to process."));

            Console.WriteLine("{0} Finishing method: RegisterMessageHandler_CanInvoke", DateTime.Now);
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
            Console.WriteLine("{0}, Starting method: RegisterMessageHandler_ThrowsOnMutipleMessageHandlerRegistrations", DateTime.Now);
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

            Console.WriteLine("{0}, Finishing method: RegisterMessageHandler_ThrowsOnMutipleMessageHandlerRegistrations", DateTime.Now);
        }

        [Fact]
        public async Task RegisterMessageHandler_ThrowsForNullMessageHandlerOptions()
        {
            Console.WriteLine("{0} Starting method: RegisterMessageHandler_ThrowsForNullMessageHandlerOptions", DateTime.Now);

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

            Console.WriteLine("{0} Finishing method: RegisterMessageHandler_ThrowsForNullMessageHandlerOptions", DateTime.Now);
        }

        [Fact]
        public async Task RegisterMessageHandler_CanSetMessageHandlerOptions()
        {
            Console.WriteLine("{0} Starting method: RegisterMessageHandler_CanSetMessageHandlerOptions", DateTime.Now);

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

            Console.WriteLine("{0} Finishing method: RegisterMessageHandler_CanSetMessageHandlerOptions", DateTime.Now);
        }

        [Fact]
        public async Task RegisterMessageHandler_ExceptionHandlerInvoked()
        {
            Console.WriteLine("{0} Starting method: RegisterMessageHandler_ExceptionHandlerInvoked", DateTime.Now);

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

            Console.WriteLine("{0} Finishing method: RegisterMessageHandler_ExceptionHandlerInvoked", DateTime.Now);
        }

        [Fact]
        public async Task RenewLockAsync_CanInvoke()
        {
            Console.WriteLine("{0} Starting method: RenewLockAsync_CanInvoke", DateTime.Now);

            // Arrange
            await _sut.EnqueueAsync(
                _fixture.CreateMessage(),
                CancellationToken.None);

            var message = await _sut.DequeueAsync(CancellationToken.None);

            // Act
            await _sut.RenewLockAsync(message, CancellationToken.None);

            // Clean up
            await _sut.CompleteAsync(message, CancellationToken.None);

            Console.WriteLine("{0} Finishing method: RenewLockAsync_CanInvoke", DateTime.Now);
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
