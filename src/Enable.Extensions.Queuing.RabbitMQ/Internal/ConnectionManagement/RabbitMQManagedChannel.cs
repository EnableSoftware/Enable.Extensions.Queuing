using System;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal.ConnectionManagement
{
    internal class RabbitMQManagedChannel
    {
        public RabbitMQManagedChannel(IModel channel, Guid connectionId)
        {
            Channel = channel;
            ConnectionId = connectionId;
        }

        public IModel Channel { get; }

        public Guid ConnectionId { get; }
    }
}
