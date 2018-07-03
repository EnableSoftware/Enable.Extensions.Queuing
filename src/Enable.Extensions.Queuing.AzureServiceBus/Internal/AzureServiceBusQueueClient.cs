using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using MessageHandlerOptions = Enable.Extensions.Queuing.Abstractions.MessageHandlerOptions;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    // TODO Respect cancellationToken
    public class AzureServiceBusQueueClient : BaseQueueClient
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly MessageReceiver _messageReceiver;
        private readonly MessageSender _messageSender;

        private bool _disposed;

        public AzureServiceBusQueueClient(
            string connectionString,
            string queueName)
        {
            _connectionString = connectionString;
            _queueName = queueName;

            _messageReceiver = new MessageReceiver(
                    connectionString,
                    queueName,
                    ReceiveMode.PeekLock);

            _messageSender = new MessageSender(connectionString, queueName);
        }

        public override Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageReceiver.AbandonAsync(message.LeaseId);
        }

        public override Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageReceiver.CompleteAsync(message.LeaseId);
        }

        public override async Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // This timeout is arbitrary. It is needed in order to return null
            // if no messages are queued, and must be long enough to allow time
            // for connection setup.
            var message = await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(10));

            if (message == null)
            {
                return null;
            }

            return new AzureServiceBusQueueMessage(message);
        }

        public override Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageSender.SendAsync(new Message(message.Body) { ContentType = "application/json" });
        }

        public override Task RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> messageHandler,
            MessageHandlerOptions messageHandlerOptions)
        {
            if (messageHandlerOptions == null)
            {
                throw new ArgumentNullException(nameof(messageHandlerOptions));
            }

            var exceptionReceivedHandler = GetExceptionReceivedHandler(messageHandlerOptions);

            var options = new Microsoft.Azure.ServiceBus.MessageHandlerOptions(exceptionReceivedHandler)
            {
                AutoComplete = messageHandlerOptions.AutoComplete,
                MaxConcurrentCalls = messageHandlerOptions.MaxConcurrentCalls,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(5)
            };

            _messageReceiver.RegisterMessageHandler(
                (message, token) => messageHandler(new AzureServiceBusQueueMessage(message), token),
                options);

            return Task.CompletedTask;
        }

        public override Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageReceiver.RenewLockAsync(message.LeaseId);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _messageReceiver.CloseAsync()
                        .GetAwaiter()
                        .GetResult();

                    _messageSender.CloseAsync()
                        .GetAwaiter()
                        .GetResult();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        private static Func<ExceptionReceivedEventArgs, Task> GetExceptionReceivedHandler(
            MessageHandlerOptions options)
        {
            if (options?.ExceptionReceivedHandler == null)
            {
                return _ => Task.CompletedTask;
            }

            Task ExceptionReceivedHandler(ExceptionReceivedEventArgs args)
            {
                var context = new MessageHandlerExceptionContext(args.Exception);

                return options.ExceptionReceivedHandler(new MessageHandlerExceptionContext(args.Exception));
            }

            return ExceptionReceivedHandler;
        }
    }
}
