using Enable.Extensions.Queuing.AzureServiceBus.Internal;
using Xunit;

namespace Enable.Extensions.Queuing.AzureServiceBus.Tests
{
    public class ConcurrentQueueClientTests : IClassFixture<AzureServiceBusTestFixture>
    {
        private readonly AzureServiceBusQueueClientFactory _factory;
        private readonly string _queueName;

        public ConcurrentQueueClientTests(AzureServiceBusTestFixture fixture)
        {
            var factoryOptions = new AzureServiceBusQueueClientFactoryOptions
            {
                ConnectionString = fixture.ConnectionString
            };

            _factory = new AzureServiceBusQueueClientFactory(factoryOptions);
            _queueName = fixture.QueueName;
        }

        [Fact]
        public void ConcurrentQueueClientsUseSameMessageReceiver()
        {
            // Arrange
            var client = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;
            var anotherClient = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;

            // Assert
            Assert.Same(client.MessageReceiver, anotherClient.MessageReceiver);

            // Clean up
            client.Dispose();
            anotherClient.Dispose();
        }

        [Fact]
        public void ConcurrentQueueClientsUseSameMessageSender()
        {
            // Arrange
            var client = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;
            var anotherClient = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;

            // Assert
            Assert.Same(client.MessageSender, anotherClient.MessageSender);

            // Clean up
            client.Dispose();
            anotherClient.Dispose();
        }

        [Fact]
        public void UnconcurrentQueueClientsUseDifferentMessageReceiver()
        {
            // Arrange
            var client = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;
            client.Dispose(); // Dispose of this client before we make another.

            var anotherClient = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;

            // Assert
            Assert.NotSame(client.MessageReceiver, anotherClient.MessageReceiver);

            // Clean up
            anotherClient.Dispose();
        }

        [Fact]
        public void UnconcurrentQueueClientsUseDifferentMessageSender()
        {
            // Arrange
            var client = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;
            client.Dispose(); // Dispose of this client before we make another.

            var anotherClient = _factory.GetQueueReference(_queueName) as AzureServiceBusQueueClient;

            // Assert
            Assert.NotSame(client.MessageSender, anotherClient.MessageSender);

            // Clean up
            anotherClient.Dispose();
        }
    }
}
