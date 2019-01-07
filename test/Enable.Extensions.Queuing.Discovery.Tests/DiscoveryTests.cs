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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
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
                o => o.RegisterMessageHandler(It.IsAny<Func<IQueueMessage, CancellationToken, Task>>(), It.IsAny<MessageHandlerOptions>()),
                Times.Once);
        }

        [Fact]
        public async Task DiscoverMessageHandlers_LogsDiscoveredHandlers()
        {
            // Arrange
            var logger = new Mock<ILogger>();
            var loggerProvider = new Mock<ILoggerProvider>();
            loggerProvider.Setup(o => o.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

            var queueClient = new Mock<IQueueClient>();
            var queueClientFactory = new Mock<IQueueClientFactory>();
            queueClientFactory.Setup(o => o.GetQueueReference(QueueName)).Returns(queueClient.Object);

            var services = new ServiceCollection();
            services.AddSingleton(new TestJob());
            services.AddSingleton(queueClientFactory.Object);
            services.AddSingleton<QueueManager>();
            services.AddLogging(builder =>
            {
                builder.AddProvider(loggerProvider.Object);
            });
            var serviceProvider = services.BuildServiceProvider();

            // Act
            await serviceProvider.DiscoverMessageHandlers(Assembly.GetAssembly(typeof(TestJob)));

            // Assert
            var expectedMessage = $"Registering discovered message handler {nameof(TestJob)}.{nameof(TestJob.RunAsync)}({nameof(TestDto)}) for queue \"{QueueName}\"";

            logger.Verify(
                o => o.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<FormattedLogValues>(v => v.ToString().Equals(expectedMessage, StringComparison.Ordinal)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DiscoverMessageHandlers_CallsMessageHandler()
        {
            // Arrange
            var fixture = new Fixture();

            IQueueClientFactory queueClientFactory = new InMemoryQueueClientFactory();
            var queueClient = queueClientFactory.GetQueueReference(QueueName);

            var message = new TestDto
            {
                TestProperty = fixture.Create<string>()
            };

            var job = new Mock<TestJob>();
            job.Setup(o => o.RunAsync(It.IsAny<TestDto>()));

            var services = new ServiceCollection();
            services.AddSingleton(job.Object);
            services.AddSingleton(queueClientFactory);
            services.AddSingleton<QueueManager>();
            var serviceProvider = services.BuildServiceProvider();

            await serviceProvider.DiscoverMessageHandlers(Assembly.GetAssembly(typeof(TestJob)));

            // Act
            await queueClient.EnqueueAsync(message);

            // Assert
            job.Verify(o => o.RunAsync(It.Is<TestDto>(p => p.Equals(message))), Times.Once);
        }

        public class TestDto : IEquatable<TestDto>
        {
            public string TestProperty { get; set; }

            public bool Equals(TestDto other)
            {
                return other != null
                    && (ReferenceEquals(this, other)
                    || string.Equals(TestProperty, other.TestProperty, StringComparison.Ordinal));
            }
        }

        public class TestJob
        {
            [MessageHandler(QueueName)]
            public virtual Task RunAsync(TestDto dto)
            {
                return Task.CompletedTask;
            }
        }
    }
}
