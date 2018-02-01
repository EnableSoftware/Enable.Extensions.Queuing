using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    public class AzureServiceBusQueueClient : Abstractions.IQueueClient
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

        public Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageReceiver.AbandonAsync(message.LeaseId);
        }

        public Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageReceiver.CompleteAsync(message.LeaseId);
        }

        public async Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = await _messageReceiver.ReceiveAsync();

            if (message == null)
            {
                return null;
            }

            return new AzureServiceBusQueueMessage(message);
        }

        public Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageSender.SendAsync(new Message(message.Body) { ContentType = "application/json" });
        }

        public Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _messageReceiver.RenewLockAsync(message.LeaseId);
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
                    _messageReceiver.CloseAsync()
                        .GetAwaiter()
                        .GetResult();

                    _messageSender.CloseAsync()
                        .GetAwaiter()
                        .GetResult();
                }

                _disposed = true;
            }
        }
    }
}
