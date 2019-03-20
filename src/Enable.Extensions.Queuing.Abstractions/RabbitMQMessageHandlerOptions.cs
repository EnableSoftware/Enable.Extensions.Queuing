namespace Enable.Extensions.Queuing.Abstractions
{
    public class RabbitMQMessageHandlerOptions : MessageHandlerOptions
    {
        public int? PrefetchCount { get; set; }
    }
}
