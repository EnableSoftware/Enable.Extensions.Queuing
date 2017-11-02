using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Enable.Extensions.Queuing.Abstractions
{
    public interface IQueueClient : IDisposable
    {
        /// <summary>
        /// Negative acknowledgement that the message was not processed correctly.
        /// </summary>
        /// <remarks>
        /// Calling this method will increment the delivery count of the message and,
        /// if the devlivery count exceeds the max delivery count of the queue,
        /// asynchronously moves the message to the dead letter queue.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Acknowledge the message has been successfully received and processed.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously retrieve a message from the queue.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously enqueue a message on to the queue.
        /// </summary>
        /// <param name="message">The message to enqueue.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));

        Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public static class IQueueExtensions
    {
        public static Task EnqueueAsync(
            this IQueueClient queue,
            byte[] content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = new QueueMessage(content);

            return queue.EnqueueAsync(message, cancellationToken);
        }

        public static Task EnqueueAsync(
            this IQueueClient queue,
            string content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return queue.EnqueueAsync<string>(content, cancellationToken);
        }

        public static Task EnqueueAsync<T>(
            this IQueueClient queue,
            T content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var json = JsonConvert.SerializeObject(content);

            var payload = Encoding.UTF8.GetBytes(json);

            var message = new QueueMessage(payload);

            return queue.EnqueueAsync(message, cancellationToken);
        }
    }
}
