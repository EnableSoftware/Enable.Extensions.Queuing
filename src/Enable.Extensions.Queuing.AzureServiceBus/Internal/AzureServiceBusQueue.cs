using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    internal class AzureServiceBusQueue : IDisposable
    {
        private int _referenceCount = 1;
        private bool _disposed = false;

        public AzureServiceBusQueue(string connectionString, string queueName, int prefetchCount)
        {
            MessageReceiver = new MessageReceiver(
                connectionString,
                queueName,
                ReceiveMode.PeekLock,
                prefetchCount: prefetchCount);

            MessageSender = new MessageSender(connectionString, queueName);
        }

        public IMessageSender MessageSender { get; }
        public IMessageReceiver MessageReceiver { get; }

        public int IncrementReferenceCount()
        {
            return Interlocked.Increment(ref _referenceCount);
        }

        public int DecrementReferenceCount()
        {
            return Interlocked.Decrement(ref _referenceCount);
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
                    MessageReceiver.CloseAsync()
                        .GetAwaiter()
                        .GetResult();

                    MessageSender.CloseAsync()
                        .GetAwaiter()
                        .GetResult();
                }

                _disposed = true;
            }
        }
    }
}
