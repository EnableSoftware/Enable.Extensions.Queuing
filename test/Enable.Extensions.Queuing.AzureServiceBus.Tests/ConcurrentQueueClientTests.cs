using System;
using System.Security;
using Enable.Extensions.Queuing.AzureServiceBus.Internal;
using Xunit;

namespace Enable.Extensions.Queuing.AzureServiceBus.Tests
{
    public class ConcurrentQueueClientTests
    {
        private readonly AzureServiceBusQueueClientFactory _factory;
        private readonly string _queueName;

        public ConcurrentQueueClientTests()
        {
            var connectionString = GetEnvironmentVariable("AZURE_SERVICE_BUS_CONNECTION_STRING");

            var factoryOptions = new AzureServiceBusQueueClientFactoryOptions
            {
                ConnectionString = connectionString
            };

            _factory = new AzureServiceBusQueueClientFactory(factoryOptions);
            _queueName = GetEnvironmentVariable("AZURE_SERVICE_BUS_QUEUE_NAME");
        }

        [Fact]
        public void ConcurrentQueueClientsUseSameQueue()
        {
            // Arrange
            var client = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;
            var anotherClient = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;

            // Assert
            Assert.True(object.ReferenceEquals(client.Queue, anotherClient.Queue));

            // Clean up
            client.Dispose();
            anotherClient.Dispose();
        }

        [Fact]
        public void UnconcurrentQueueClientsUseDifferentQueue()
        {
            // Arrange
            var client = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;
            client.Dispose(); // Dispose of this client before we make another.

            var anotherClient = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;

            // Assert
            Assert.False(object.ReferenceEquals(client.Queue, anotherClient.Queue));

            // Clean up
            anotherClient.Dispose();
        }

        private static string GetEnvironmentVariable(string name)
        {
            try
            {
                var value = Environment.GetEnvironmentVariable(name);

                if (value == null)
                {
                    throw new Exception($"The environment variable '{name}' could not be found.");
                }

                return value;
            }
            catch (SecurityException ex)
            {
                throw new Exception($"The environment variable '{name}' is not accessible.", ex);
            }
        }
    }
}
