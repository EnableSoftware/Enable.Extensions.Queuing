namespace Enable.Extensions.Queuing.Abstractions
{
    /// <summary>
    /// Represents a message retrieved from a queue.
    /// </summary>
    public interface IQueueMessage
    {
        /// <summary>
        /// Gets the message ID.
        /// </summary>
        string MessageId { get; }

        /// <summary>
        /// Gets a lease token for the current message.
        /// </summary>
        /// <remarks>
        /// When messages are retrived from a queue, they are "locked" for a period of time.
        /// This gives the consumer of the message a fixed amount of time in order to complete,
        /// see <see cref="IQueueClient.CompleteAsync(IQueueMessage, System.Threading.CancellationToken)"/>,
        /// or abandon, <see cref="IQueueClient.AbandonAsync(IQueueMessage, System.Threading.CancellationToken)"/>,
        /// the processing of the message. After this time messages are returned to the queue and can
        /// be consumed by other processes.
        /// </remarks>
        string LeaseId { get; }

        /// <summary>
        /// Gets the number of times this message has been dequeued.
        /// </summary>
        uint DequeueCount { get; }

        /// <summary>
        /// Gets the content of the current message.
        /// </summary>
        byte[] Body { get; }
    }
}
