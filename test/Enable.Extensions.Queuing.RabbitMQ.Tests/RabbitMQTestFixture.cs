using System;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Tests
{
    public class RabbitMQTestFixture
    {
        public string HostName { get; } = "localhost";

        public int Port { get; } = 5672;

        public string VirtualHost { get; } = ConnectionFactory.DefaultVHost;

        public string UserName { get; } = ConnectionFactory.DefaultUser;

        public string Password { get; } = ConnectionFactory.DefaultPass;

        public void DeleteQueue(string queueName)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = HostName,
                Port = Port,
                VirtualHost = VirtualHost,
                UserName = UserName,
                Password = Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDelete(queueName, ifUnused: false, ifEmpty: false);
            }
        }
    }
}
