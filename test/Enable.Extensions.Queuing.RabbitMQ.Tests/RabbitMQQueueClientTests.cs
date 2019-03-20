using System;
using System.Collections.Generic;
using System.Text;
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

            _fixture = fixture;
        }

        [Fact]
        public async Task EnqueueAsync_CanInvokeWithString()
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
        public async Task EnqueueAsync_CanInvokeWithByteArray()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());

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
                Message = Guid.NewGuid().ToString()
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
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
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
        public async Task RegisterMessageHandler_CanSetPrefectCount()
        {
            // Arrange
            Task MessageHandler(IQueueMessage message, CancellationToken cancellationToken)
            {
                throw new Exception("There should be no messages to process.");
            }

            var options = new RabbitMQMessageHandlerOptions
            {
                MaxConcurrentCalls = 1,
                ExceptionReceivedHandler = (_) => Task.CompletedTask,
                PrefetchCount = 2
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
            var exception = await Record.ExceptionAsync(() => _sut.RenewLockAsync(message, CancellationToken.None));

            // Assert
            Assert.IsType<NotImplementedException>(exception);

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
                    // With the RabbitMQ implementation, we must disconnect
                    // our consumer before purging the queue, otherwise,
                    // purging the queue won't remove unacked messages.
                    _sut.Dispose();

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

        private class CustomMessageType
        {
            public string Message { get; set; }
        }
    }
}
