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
    internal class BaseRabbitMQQueueClient : BaseQueueClient
    {
        private bool _messageHandlerRegistered;
        private bool _disposed;

        public BaseRabbitMQQueueClient(
            ConnectionFactory connectionFactory,
            string queueName,
            QueueMode queueMode = QueueMode.Default)
        {
            ConnectionFactory = connectionFactory;
            Connection = ConnectionFactory.CreateConnection();
            Channel = Connection.CreateModel();

            // Declare the dead letter queue.
            DeadLetterQueueName = GetDeadLetterQueueName(queueName);
            DLQueueArguments = null;

            ExchangeName = string.Empty;

            // Declare the main queue.
            QueueName = queueName;

            QueueArguments = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", string.Empty },
                { "x-dead-letter-routing-key", DeadLetterQueueName },
            };

            if (queueMode == QueueMode.Lazy)
            {
                QueueArguments.Add("x-queue-mode", "lazy");
            }
        }

        protected string ExchangeName { get; set; }
        protected ConnectionFactory ConnectionFactory { get; set; }
        protected IConnection Connection { get; set; }
        protected IModel Channel { get; set; }
        protected string QueueName { get; set; }
        protected string DeadLetterQueueName { get; set; }
        protected Dictionary<string, object> QueueArguments { get; set; }
        protected Dictionary<string, object> DLQueueArguments { get; set; }

        public override Task AbandonAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var deliveryTag = Convert.ToUInt64(message.LeaseId);

            // Lock the channel before attempting to send an acknowledgement.
            // This is required because `IModel` is not thread safe.
            // See https://www.rabbitmq.com/dotnet-api-guide.html#model-sharing.
            lock (Channel)
            {
                // TODO Handle automatic dead-lettering after a certain number of requeues.
                // Storing a delivery count is on the roadmap for RabbitMQ 3.8, see
                // https://github.com/rabbitmq/rabbitmq-server/issues/502 for more information.
                // In the meantime, we settle for dead lettering a message on the first negative
                // acknowledgement.
                Channel.BasicReject(deliveryTag, requeue: false);
            }

            return Task.CompletedTask;
        }

        public override Task CompleteAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var deliveryTag = Convert.ToUInt64(message.LeaseId);

            lock (Channel)
            {
                Channel.BasicAck(deliveryTag, multiple: false);
            }

            return Task.CompletedTask;
        }

        public override Task<IQueueMessage> DequeueAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            BasicGetResult result;

            lock (Channel)
            {
                result = Channel.BasicGet(QueueName, autoAck: false);
            }

            if (result == null)
            {
                return Task.FromResult<IQueueMessage>(null);
            }

            var message = new RabbitMQQueueMessage(
                result.Body.ToArray(),
                result.DeliveryTag,
                result.BasicProperties);

            return Task.FromResult<IQueueMessage>(message);
        }

        public override Task EnqueueAsync(
            IQueueMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var body = message.Body;
            var messageProperties = GetBasicMessageProperties(Channel);

            lock (Channel)
            {
                Channel.BasicPublish(
                    ExchangeName,
                    QueueName,
                    messageProperties,
                    body);
            }

            return Task.CompletedTask;
        }

        public override Task EnqueueAsync(
            IEnumerable<IQueueMessage> messages,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Setting the mandatory option to false will silently drop a message
            // if it fails. This is the default value for the BasicPublish method,
            // which is used elsewhere when publishing single messages.
            var mandatory = false;
            var messageProperties = GetBasicMessageProperties(Channel);

            lock (Channel)
            {
                var batch = Channel.CreateBasicPublishBatch();

                foreach (var message in messages)
                {
                    batch.Add(
                        ExchangeName,
                        QueueName,
                        mandatory,
                        messageProperties,
                        new ReadOnlyMemory<byte>(message.Body));
                }

                batch.Publish();
            }

            return Task.CompletedTask;
        }

        public override Task RegisterMessageHandler(
            Func<IQueueMessage, CancellationToken, Task> messageHandler,
            MessageHandlerOptions messageHandlerOptions)
        {
            if (messageHandlerOptions == null)
            {
                throw new ArgumentNullException(nameof(messageHandlerOptions));
            }

            if (messageHandlerOptions.MaxConcurrentCalls > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(messageHandlerOptions.MaxConcurrentCalls),
                    messageHandlerOptions.MaxConcurrentCalls,
                    $@"'{nameof(messageHandlerOptions.MaxConcurrentCalls)}' must be less than or equal to {ushort.MaxValue}.");
            }

            lock (Channel)
            {
                if (_messageHandlerRegistered)
                {
                    throw new InvalidOperationException("A message handler has already been registered.");
                }

                // Reconfigure quality of service on the channel in order to
                // support specified level of concurrency. Here we are changing
                // the prefetch count to a user-specified `MaxConcurrentCalls`.
                // This only affects new consumers on the channel, existing
                // consumers are unaffected and will have a `prefetchCount` of 1,
                // as specified in the constructor, above.
                ConfigureBasicQos(prefetchCount: messageHandlerOptions.MaxConcurrentCalls);

                var consumer = new EventingBasicConsumer(Channel);

                consumer.Received += (channel, eventArgs) =>
                {
                    // Queue the processing of the message received. This allows
                    // the calling consumer instance to continue before the message
                    // handler completes, which is essential for handling multiple
                    // messages concurrently with a single consumer.
                    Task.Run(() => OnMessageReceivedAsync(messageHandler, messageHandlerOptions, eventArgs)).Ignore();
                };

                Channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);

                _messageHandlerRegistered = true;
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
                    Channel.Dispose();
                    Connection.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        protected void DeclareQueues()
        {
            Channel.QueueDeclare(
                DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: DLQueueArguments);

            Channel.QueueDeclare(
               QueueName,
               durable: true,
               exclusive: false,
               autoDelete: false,
               arguments: QueueArguments);

            // Here we are assuming a "worker queue", where multiple consumers are
            // competing to draw from a single queue in order to spread workload
            // across the consumers.
            ConfigureBasicQos(prefetchCount: 1);
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

        private void ConfigureBasicQos(int prefetchCount)
        {
            lock (Channel)
            {
                // Here we are assuming a "worker queue", where multiple consumers are
                // competing to draw from a single queue in order to spread workload
                // across the consumers.
                Channel.BasicQos(
                    prefetchSize: 0,
                    prefetchCount: Convert.ToUInt16(prefetchCount),
                    global: false);
            }
        }

        private async Task OnMessageReceivedAsync(
            Func<IQueueMessage, CancellationToken, Task> messageHandler,
            MessageHandlerOptions messageHandlerOptions,
            BasicDeliverEventArgs eventArgs)
        {
            var cancellationToken = CancellationToken.None;

            var message = new RabbitMQQueueMessage(
                eventArgs.Body.ToArray(),
                eventArgs.DeliveryTag,
                eventArgs.BasicProperties);

            try
            {
                await messageHandler(message, cancellationToken);

                if (messageHandlerOptions.AutoComplete)
                {
                    await CompleteAsync(message, cancellationToken);
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

                        await exceptionHandler(context);
                    }
                }
                catch
                {
                }

                await AbandonAsync(message, cancellationToken);
            }
        }
    }
}
