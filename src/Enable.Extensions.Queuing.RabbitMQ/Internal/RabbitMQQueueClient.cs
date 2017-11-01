using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQQueueClient : IQueueClient
    {
        private const string RedeliveryCountHeaderName = "x-redelivered-count";

        private readonly ConnectionFactory _connectionFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName;
        private readonly string _queueName;
        private readonly string _deadLetterQueueName;

        private bool _disposed;

        public RabbitMQQueueClient(ConnectionFactory connectionFactory, string queueName)
        {
            _connectionFactory = connectionFactory;
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare the dead letter queue.
            _deadLetterQueueName = GetDeadLetterQueueName(queueName);

            _channel.QueueDeclare(
                _deadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _exchangeName = string.Empty;

            // Declare the main queue.
            _queueName = queueName;

            var queueArguments = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", string.Empty },
                { "x-dead-letter-routing-key", _deadLetterQueueName }
            };

            _channel.QueueDeclare(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments);

            // Here we are assuming a "worker queue", where multiple consumers are
            // competing to draw from a single queue in order to spread workload
            // across the consumers.
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        public Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var deliveryTag = Convert.ToUInt64(message.LeaseId);

            // Lock the channel before attempting to send an acknowledgement.
            // This is required because `IModel` is not thread safe.
            // See https://www.rabbitmq.com/dotnet-api-guide.html#model-sharing.
            lock (_channel)
            {
                // TODO Handle automatic dead-lettering after a certain number of requeues.
                // Storing a delivery count is on the roadmap for RabbitMQ 3.8, see
                // https://github.com/rabbitmq/rabbitmq-server/issues/502 for more information.
                // In the meantime, we settle for dead lettering a message on the first negative
                // acknowledgement.
                _channel.BasicReject(deliveryTag, requeue: false);
            }

            return Task.CompletedTask;
        }

        public Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var deliveryTag = Convert.ToUInt64(message.LeaseId);

            lock (_channel)
            {
                _channel.BasicAck(deliveryTag, multiple: false);
            }

            return Task.CompletedTask;
        }

        public Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BasicGetResult result;

            lock (_channel)
            {
                result = _channel.BasicGet(_queueName, autoAck: false);
            }

            var message = new RabbitMQQueueMessage(result);

            return Task.FromResult<IQueueMessage>(message);
        }

        public Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var body = message.Body;
            var messageProperties = GetBasicMessageProperties(_channel);

            lock (_channel)
            {
                _channel.BasicPublish(
                    _exchangeName,
                    _queueName,
                    messageProperties,
                    body);
            }

            return Task.CompletedTask;
        }

        public Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO Implement automatic `nack`-ing.
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _channel.Dispose();
                    _connection.Dispose();
                }

                _disposed = true;
            }
        }

        private static IBasicProperties GetBasicMessageProperties(IModel channel)
        {
            var properties = channel.CreateBasicProperties();

            properties.ContentEncoding = Encoding.UTF8.HeaderName;
            properties.ContentType = "application/json";
            properties.Persistent = true;

            return properties;
        }

        private string GetDeadLetterQueueName(string queueName)
        {
            return $"{queueName}.dead-letter";
        }
    }
}
