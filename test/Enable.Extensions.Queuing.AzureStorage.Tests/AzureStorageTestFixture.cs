using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Enable.Extensions.Queuing.AzureStorage.Tests
{
    public class AzureStorageTestFixture
    {
        public AzureStorageTestFixture()
        {
            AccountName = GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME");

            AccountKey = GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_KEY");
        }

        public string AccountName { get; private set; }

        public string AccountKey { get; private set; }

        public Task DeleteQueue(string queueName)
        {
            var credentials = new StorageCredentials(AccountName, AccountKey);
            var storageAccount = new CloudStorageAccount(credentials, useHttps: true);

            var client = storageAccount.CreateCloudQueueClient();

            var queue = client.GetQueueReference(queueName);

            return queue.DeleteIfExistsAsync();
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
    }
}
