namespace Enable.Extensions.Queuing.Abstractions
{
    public interface IQueueClientFactory
    {
        IQueueClient GetQueueReference(string queueName, QueueOptions queueOptions = null);
    }
}
