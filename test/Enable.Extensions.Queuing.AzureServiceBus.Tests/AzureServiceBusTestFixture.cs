using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Enable.Extensions.Queuing.AzureServiceBus.Tests
{
    public class AzureServiceBusTestFixture
    {
        public AzureServiceBusTestFixture()
        {
            ConnectionString = GetEnvironmentVariable("AZURE_SERVICE_BUS_CONNECTION_STRING");

            QueueName = GetEnvironmentVariable("AZURE_SERVICE_BUS_QUEUE_NAME");
        }

        public string ConnectionString { get; private set; }

        public string QueueName { get; private set; }

        public async Task ClearQueue()
        {
            var messageReceiver = new MessageReceiver(
                ConnectionString,
                QueueName,
                ReceiveMode.ReceiveAndDelete);

            messageReceiver.PrefetchCount = 256;

            IList<Message> messages;

            do
            {
                messages = await messageReceiver.ReceiveAsync(
                    maxMessageCount: 256,
                    operationTimeout: TimeSpan.FromSeconds(10));
            }
            while (messages?.Count > 0);

            await messageReceiver.CloseAsync();

            messageReceiver = null;
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
