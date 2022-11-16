using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQueueClient : BaseRabbitMQQueueClient
    {
        public RabbitMQQueueClient(
            ConnectionFactory connectionFactory,
            string queueName,
            QueueMode queueMode = QueueMode.Default)
            : base(connectionFactory, queueName, queueMode)
        {
            DeclareQueues();
        }
    }
}
