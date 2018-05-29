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
    internal class RabbitMQQueueClient : BaseQueueClient
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

        public override Task AbandonAsync(
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

        public override Task CompleteAsync(
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

        public override Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BasicGetResult result;

            lock (_channel)
            {
                result = _channel.BasicGet(_queueName, autoAck: false);
            }

            if (result == null)
            {
                return Task.FromResult<IQueueMessage>(null);
            }

            var message = new RabbitMQQueueMessage(
                result.Body,
                result.DeliveryTag,
                result.BasicProperties);

            return Task.FromResult<IQueueMessage>(message);
        }

        public override Task EnqueueAsync(
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

        public override Task RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> messageHandler,
            MessageHandlerOptions messageHandlerOptions)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (channel, eventArgs) =>
            {
                var cancellationToken = CancellationToken.None;

                var message = new RabbitMQQueueMessage(
                    eventArgs.Body,
                    eventArgs.DeliveryTag,
                    eventArgs.BasicProperties);

                try
                {
                    messageHandler(message, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                    
                    if (messageHandlerOptions.AutoComplete)
                    {
                        CompleteAsync(message, cancellationToken)
                            .GetAwaiter()
                            .GetResult();
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        var exceptionHandler = messageHandlerOptions?.ExceptionReceivedHandler;

                        if (exceptionHandler != null)
                        {
                            var context = new MessageHandlerExceptionContext(ex);

                            exceptionHandler(context)
                                .GetAwaiter()
                                .GetResult();
                        }
                    }
                    catch
                    {
                    }

                    AbandonAsync(message, cancellationToken)
                        .GetAwaiter()
                        .GetResult();
                }
            };

            lock (_channel)
            {
                _channel.BasicConsume(_queueName, autoAck: false, consumer: consumer);
            }

            return Task.CompletedTask;
        }

        public override Task RenewLockAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO Implement automatic `nack`-ing.
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
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

            base.Dispose(disposing);
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
