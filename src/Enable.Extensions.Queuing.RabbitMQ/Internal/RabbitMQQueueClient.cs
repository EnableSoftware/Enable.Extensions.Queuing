using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
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

        private readonly IMemoryCache _unacknowledgedMessages;

        private bool _disposed;

        public RabbitMQQueueClient(ConnectionFactory connectionFactory, string queueName)
        {
            _connectionFactory = connectionFactory;
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _unacknowledgedMessages = new MemoryCache(new MemoryCacheOptions());

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

            _unacknowledgedMessages.Remove(deliveryTag);

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

            _unacknowledgedMessages.Remove(deliveryTag);

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

            RegisterUnacknowledgedMessage(message);

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
            // TODO Test the case that the connection is dropped. Should we clear our local memory cache?
            RegisterUnacknowledgedMessage(message);

            return Task.CompletedTask;
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
                    _unacknowledgedMessages.Dispose();
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

        private void RegisterUnacknowledgedMessage(IQueueMessage message)
        {
            var cts = new CancellationTokenSource();

            // Here we use a timer on a `CancellationTokenSource` to trigger
            // the removal of a delivery tag after a given period of time
            // instead of using an absolute expiration. This is because items
            // with an absolute expiration are only removed if there is
            // activity on the cache: there is no background process that
            // removes expired items. For more information, see
            // https://github.com/aspnet/Caching/issues/248.
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove)
                .RegisterPostEvictionCallback(OnLeaseExpired)
                .AddExpirationToken(new CancellationChangeToken(cts.Token));

            _unacknowledgedMessages.Set(
                message.LeaseId,
                message,
                cacheEntryOptions);

            cts.CancelAfter(TimeSpan.FromMinutes(1));
        }

        private void OnLeaseExpired(object key, object value, EvictionReason reason, object state)
        {
            switch (reason)
            {
                case EvictionReason.TokenExpired:
                    var deliveryTag = Convert.ToUInt64(key);

                    lock (_channel)
                    {
                        _channel.BasicReject(deliveryTag, requeue: true);
                    }

                    break;

                case EvictionReason.Capacity:
                case EvictionReason.Expired:
                    throw new NotSupportedException();

                case EvictionReason.None:
                case EvictionReason.Removed:
                case EvictionReason.Replaced:
                default:
                    break;
            }
        }
    }
}
