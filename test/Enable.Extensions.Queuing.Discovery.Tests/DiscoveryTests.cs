using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Enable.Extensions.Queuing.Discovery.Tests
{
    public class DiscoveryTests
    {
        private const string QueueName = "LoremIpsum";

        [Fact]
        public async Task DiscoverMessageHandlers_RegistersMessageHandler()
        {
            // Arrange
            var queueClient = new Mock<IQueueClient>();
            var queueClientFactory = new Mock<IQueueClientFactory>();
            queueClientFactory.Setup(o => o.GetQueueReference(QueueName)).Returns(queueClient.Object);

            var job = new TestJob();

            var services = new ServiceCollection();
            services.AddSingleton(job);
            services.AddSingleton(queueClientFactory.Object);
            services.AddSingleton<QueueManager>();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            await serviceProvider.DiscoverMessageHandlers(Assembly.GetAssembly(typeof(TestJob)));

            // Assert
            queueClientFactory.Verify(
                o => o.GetQueueReference(QueueName),
                Times.Once);

            queueClient.Verify(
                o => o.RegisterMessageHandler(It.IsAny<Func<IQueueMessage, CancellationToken, Task>>()),
                Times.Once);
        }

        [Fact]
        public async Task DiscoverMessageHandlers_CallsMessageHandler()
        {
            // Arrange
            var fixture = new Fixture();

            IQueueClientFactory queueClientFactory = new InMemoryQueueClientFactory();
            var queueClient = queueClientFactory.GetQueueReference(QueueName);

            var job = new Mock<TestJob>();
            job.Setup(o => o.RunAsync(It.IsAny<TestDto>()));

            var services = new ServiceCollection();
            services.AddSingleton(job);
            services.AddSingleton(queueClientFactory);
            services.AddSingleton<QueueManager>();
            var serviceProvider = services.BuildServiceProvider();

            await serviceProvider.DiscoverMessageHandlers(Assembly.GetAssembly(typeof(TestJob)));

            var message = new TestDto
            {
                TestProperty = fixture.Create<string>()
            };

            // Act
            await queueClient.EnqueueAsync(message);

            // Assert
            job.Verify(o => o.RunAsync(It.IsAny<TestDto>()), Times.Once);
        }

        private class TestDto
        {
            public string TestProperty { get; set; }
        }

        private class TestJob
        {
            [MessageHandler(QueueName)]
            public virtual Task RunAsync(TestDto dto)
            {
                return Task.CompletedTask;
            }
        }
    }
}
