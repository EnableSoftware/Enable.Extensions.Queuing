using System;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// Asynchronously enqueue a message on to the queue.
        /// </summary>
        /// <param name="content">The payload of the message to enqueue.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task EnqueueAsync(
             byte[] content,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously enqueue a message on to the queue.
        /// </summary>
        /// <param name="content">The payload of the message to enqueue.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task EnqueueAsync(
            string content,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously enqueue a message on to the queue.
        /// </summary>
        /// <typeparam name="T">The type of the payload to enqueue.</typeparam>
        /// <param name="content">The payload of the message to enqueue.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to observe while waiting for a task to complete.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task EnqueueAsync<T>(
            T content,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Register a message handler. This handler is awaited each time that
        /// a new message is received.
        /// </summary>
        /// <param name="handler">The handler that processes each message.</param>
        /// <remarks>A new thread is started to receive messages.</remarks>
        Task RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> handler);

        Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
