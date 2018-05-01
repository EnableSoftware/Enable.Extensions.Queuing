using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    public class AzureServiceBusQueueClient : BaseQueueClient
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly MessageReceiver _messageReceiver;
        private readonly MessageSender _messageSender;

        private bool _disposed;

        public AzureServiceBusQueueClient(string connectionString, string queueName)
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
            var message = await _messageReceiver.ReceiveAsync();

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
            var options = new MessageHandlerOptions((args) => Task.CompletedTask)
            {
                AutoComplete = false,
                MaxConcurrentCalls = 1, // TODO This value should be configurable.
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
