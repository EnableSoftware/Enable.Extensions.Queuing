using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    // TODO Respect cancellationToken
    public class AzureServiceBusQueueClient : BaseQueueClient
    {
        private readonly string _connectionString;
        private readonly string _entityName;
        private readonly int _maxConcurrentCalls;
        private readonly Func<ExceptionReceivedEventArgs, Task> _exceptionReceivedHandler;
        private readonly MessageReceiver _messageReceiver;
        private readonly MessageSender _messageSender;

        private bool _disposed;

        public AzureServiceBusQueueClient(
            string connectionString,
            string entityName,
            AzureServiceBusQueueClientOptions options)
        {
            _connectionString = connectionString;
            _entityName = entityName;
            _maxConcurrentCalls = options?.MaxConcurrentCalls ?? 0;
            _exceptionReceivedHandler = options?.ExceptionReceivedHandler ?? ((args) => Task.CompletedTask);

            _messageReceiver = new MessageReceiver(
                    connectionString,
                    entityName,
                    ReceiveMode.PeekLock);

            _messageSender = new MessageSender(connectionString, entityName);
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
            // This timeout is arbitrary. It is needed in order to return null if no message,
            // and must be long enough to allow time for connection setup.
            var message = await _messageReceiver.ReceiveAsync(TimeSpan.FromSeconds(3));

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
            Func<IQueueMessage, CancellationToken, Task> handler)
        {
            var options = new MessageHandlerOptions(_exceptionReceivedHandler)
            {
                AutoComplete = true,
                MaxConcurrentCalls = _maxConcurrentCalls,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(5)
            };

            _messageReceiver.RegisterMessageHandler(
                (message, token) => handler(new AzureServiceBusQueueMessage(message), token),
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
    }
}
