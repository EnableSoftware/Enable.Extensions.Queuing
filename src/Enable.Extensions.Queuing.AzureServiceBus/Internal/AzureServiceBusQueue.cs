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

        public AzureServiceBusQueue(string connectionString, string queueName)
        {
            Receiver = new MessageReceiver(
                connectionString,
                queueName,
                ReceiveMode.PeekLock);

            Sender = new MessageSender(connectionString, queueName);
        }

        public IMessageSender Sender { get; }
        public IMessageReceiver Receiver { get; }

        public int AddReference()
        {
            return Interlocked.Increment(ref _referenceCount);
        }

        public int RemoveReference()
        {
            return Interlocked.Decrement(ref _referenceCount);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Receiver.CloseAsync().GetAwaiter().GetResult();
                    Sender.CloseAsync().GetAwaiter().GetResult();
                }

                _disposed = true;
            }
        }
    }
}
