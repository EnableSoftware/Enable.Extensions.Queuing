using System;
using System.Security;

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
