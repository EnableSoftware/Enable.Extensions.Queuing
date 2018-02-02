using System;
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

        public abstract void Dispose();

        public abstract Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        public Task EnqueueAsync(
            byte[] content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = new QueueMessage(content);

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
            var json = JsonConvert.SerializeObject(content);

            var payload = Encoding.UTF8.GetBytes(json);

            IQueueMessage message = new QueueMessage(payload);

            return EnqueueAsync(message, cancellationToken);
        }

        public abstract Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
