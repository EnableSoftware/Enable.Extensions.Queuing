using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Enable.Extensions.Queuing.AzureStorage.Tests
{
    public class AzureStorageTestFixture : IDisposable
    {
        private bool _disposed;

        public AzureStorageTestFixture()
        {
            AccountName = GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME");

            AccountKey = GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_KEY");
        }

        public string AccountName { get; private set; }

        public string AccountKey { get; private set; }

        public string QueueName { get; } = Guid.NewGuid().ToString();

        public Task ClearQueue()
        {
            var credentials = new StorageCredentials(AccountName, AccountKey);
            var storageAccount = new CloudStorageAccount(credentials, useHttps: true);

            var client = storageAccount.CreateCloudQueueClient();

            var queue = client.GetQueueReference(QueueName);

            return queue.ClearAsync();
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
                    try
                    {
                        // Make a best effort to remove our temporary test queue.
                        DeleteQueue();
                    }
                    catch
                    {
                    }
                }

                _disposed = true;
            }
        }

        private static string GetEnvironmentVariable(string name)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable(name);

                if (connectionString == null)
                {
                    throw new Exception($"The environment variable '{name}' could not be found.");
                }

                return connectionString;
            }
            catch (SecurityException ex)
            {
                throw new Exception($"The environment variable '{name}' is not accessible.", ex);
            }
        }

         private Task DeleteQueue()
        {
            var credentials = new StorageCredentials(AccountName, AccountKey);
            var storageAccount = new CloudStorageAccount(credentials, useHttps: true);

            var client = storageAccount.CreateCloudQueueClient();

            var queue = client.GetQueueReference(QueueName);

            return queue.DeleteIfExistsAsync();
        }
    }
}
