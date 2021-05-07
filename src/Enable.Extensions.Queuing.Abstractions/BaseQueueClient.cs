using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Enable.Extensions.Queuing.Abstractions
{
    public abstract class BaseQueueClient : IQueueClient
    {
        public abstract Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken));

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task EnqueueAsync(
            IEnumerable<IQueueMessage> messages,
            CancellationToken cancellationToken = default(CancellationToken));

        public Task EnqueueAsync(
            byte[] content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            IQueueMessage message = new QueueMessage(content);

            return EnqueueAsync(message, cancellationToken);
        }

        public Task EnqueueAsync(
            string content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return EnqueueAsync<string>(content, cancellationToken);
        }

        public Task EnqueueAsync<T>(
            T content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = SerializeQueueMessage(content);

            return EnqueueAsync(message, cancellationToken);
        }

        public Task EnqueueAsync<T>(
            IEnumerable<T> messages,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var batch = new List<IQueueMessage>();

            foreach (var message in messages)
            {
                batch.Add(SerializeQueueMessage(message));
            }

            return EnqueueAsync(messages: batch, cancellationToken: cancellationToken);
        }

        public Task EnqueueAsync(
            byte[] content,
            string sessionId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            IQueueMessage message = new QueueMessage(content, sessionId);

            return EnqueueAsync(message, cancellationToken);
        }

        public Task EnqueueAsync(
            string content,
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            return EnqueueAsync<string>(content, sessionId, cancellationToken);
        }

        public Task EnqueueAsync<T>(
            T content,
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            var message = SerializeQueueMessage(content, sessionId);

            return EnqueueAsync(message, cancellationToken);
        }

        public Task EnqueueAsync<T>(
            IEnumerable<T> messages,
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            var batch = new List<IQueueMessage>();

            foreach (var message in messages)
            {
                batch.Add(SerializeQueueMessage(message, sessionId));
            }

            return EnqueueAsync(messages: batch, cancellationToken: cancellationToken);
        }

        public Task RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> messageHandler)
        {
            var messageHandlerOptions = new MessageHandlerOptions();

            return RegisterMessageHandler(messageHandler, messageHandlerOptions);
        }

        public abstract Task RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> messageHandler,
            MessageHandlerOptions messageHandlerOptions);

        public abstract Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        protected virtual void Dispose(bool disposing)
        {
        }

        private IQueueMessage SerializeQueueMessage<T>(T content)
        {
            var json = JsonConvert.SerializeObject(content);

            var payload = Encoding.UTF8.GetBytes(json);

            return new QueueMessage(payload);
        }

        private IQueueMessage SerializeQueueMessage<T>(T content, string sessionId)
        {
            var json = JsonConvert.SerializeObject(content);

            var payload = Encoding.UTF8.GetBytes(json);

            return new QueueMessage(payload, sessionId);
        }
    }
}
