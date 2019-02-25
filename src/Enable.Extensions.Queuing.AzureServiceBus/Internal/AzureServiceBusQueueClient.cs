using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private static readonly ConcurrentDictionary<int, AzureServiceBusQueue> _queues
            = new ConcurrentDictionary<int, AzureServiceBusQueue>();

        private readonly int _queueKey;
        private readonly AzureServiceBusQueue _queue;
        private bool _disposed;

        public AzureServiceBusQueueClient(
            string connectionString,
            string queueName)
        {
            _queueKey = $"{connectionString}:{queueName}".GetHashCode();

            _queue = _queues.AddOrUpdate(
                _queueKey,
                new AzureServiceBusQueue(connectionString, queueName),
                (_, queue) =>
                {
                    queue.AddReference();
                    return queue;
                });
        }

        public override Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _queue.Receiver.AbandonAsync(message.LeaseId);
        }

        public override Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _queue.Receiver.CompleteAsync(message.LeaseId);
        }

        public override async Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // This timeout is arbitrary. It is needed in order to return null
            // if no messages are queued, and must be long enough to allow time
            // for connection setup.
            var message = await _queue.Receiver.ReceiveAsync(TimeSpan.FromSeconds(10));

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
            return _queue.Sender.SendAsync(CreateMessage(message));
        }

        public override Task EnqueueAsync(
            IEnumerable<IQueueMessage> messages,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var messageList = new List<Message>();

            foreach (var message in messages)
            {
                messageList.Add(CreateMessage(message));
            }

            return _queue.Sender.SendAsync(messageList);
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

            _queue.Receiver.RegisterMessageHandler(
                (message, token) => messageHandler(new AzureServiceBusQueueMessage(message), token),
                options);

            return Task.CompletedTask;
        }

        public override Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _queue.Receiver.RenewLockAsync(message.LeaseId);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Use Monitor to prevent race conditions
                    Monitor.Enter(_queues);
                    try
                    {
                        var refCount = _queue.RemoveReference();
                        if (refCount == 0)
                        {
                            var removed = _queues.TryRemove(_queueKey, out _);
                            if (removed)
                            {
                                _queue.Dispose();
                            }
                            else
                            {
                                // This case should never occur, because we used Monitor to avoid race conditions
                                // Raise an exception if this occurs to notify us that this code is probably wrong
                                throw new Exception("Queue could not be removed from dictionary.");
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_queues);
                    }

                    base.Dispose(true);
                }

                _disposed = true;
            }
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

        private Message CreateMessage(IQueueMessage message)
        {
            return new Message(message.Body)
            {
                ContentType = "application/json"
            };
        }
    }
}
