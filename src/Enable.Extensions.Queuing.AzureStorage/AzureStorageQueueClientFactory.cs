using System;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.AzureStorage.Internal;

namespace Enable.Extensions.Queuing.AzureStorage
{
    public class AzureStorageQueueClientFactory : IQueueClientFactory
    {
        private readonly AzureStorageQueueClientFactoryOptions _options;

        public AzureStorageQueueClientFactory(AzureStorageQueueClientFactoryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.AccountName))
            {
                throw new ArgumentNullException(nameof(options.AccountName));
            }

            if (string.IsNullOrEmpty(options.AccountKey))
            {
                throw new ArgumentNullException(nameof(options.AccountKey));
            }

            _options = options;
        }

        public IQueueClient GetQueueReference(string queueName, string deadLetterQueueName = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            return new AzureStorageQueueClient(
                _options.AccountName,
                _options.AccountKey,
                queueName,
                deadLetterQueueName);
        }
    }
}
