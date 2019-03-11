# Enable.Extensions.Queuing

[![Build status](https://ci.appveyor.com/api/projects/status/0debl4iitj086eto/branch/master?svg=true)](https://ci.appveyor.com/project/EnableSoftware/enable-extensions-queuing/branch/master)

Messaging queue abstractions for building testable applications.

Queues provide asynchronous message communication. They allow applications to
easily scale out workloads to multiple processors, improve the resiliency of
applications to sudden peaks in workloads and naturally lead to loosely coupled
components or applications.

`Enable.Extensions.Queuing` currently provides four queue implementations:

- [`Enable.Extensions.Queuing.InMemory`]: An in memory queue implementation.
  This queue implementation is only valid for the lifetime of the host process.
  This is great for use in test code.

- [`Enable.Extensions.Queuing.RabbitMQ`]: A [RabbitMQ] implementation.

- [`Enable.Extensions.Queuing.AzureServiceBus`]: An [Azure Service Bus] implementation.

- [`Enable.Extensions.Queuing.AzureStorage`]: An [Azure Storage] implementation.

In addition to these packages, an additional [`Enable.Extensions.Queuing.Abstractions`]
package is available. This contains the basic abstractions that the implementations
listed above build upon. Use [`Enable.Extensions.Queuing.Abstractions`] to implement
your own queue provider.

Package name                                | NuGet version
--------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
`Enable.Extensions.Queuing.Abstractions`    | [![NuGet](https://img.shields.io/nuget/v/Enable.Extensions.Queuing.Abstractions.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Enable.Extensions.Queuing.Abstractions/)
`Enable.Extensions.Queuing.InMemory`        | [![NuGet](https://img.shields.io/nuget/v/Enable.Extensions.Queuing.InMemory.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Enable.Extensions.Queuing.InMemory/)
`Enable.Extensions.Queuing.RabbitMQ`        | [![NuGet](https://img.shields.io/nuget/v/Enable.Extensions.Queuing.RabbitMQ.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Enable.Extensions.Queuing.RabbitMQ/)
`Enable.Extensions.Queuing.AzureServiceBus` | [![NuGet](https://img.shields.io/nuget/v/Enable.Extensions.Queuing.AzureServiceBus.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Enable.Extensions.Queuing.AzureServiceBus/)
`Enable.Extensions.Queuing.AzureStorage`    | [![NuGet](https://img.shields.io/nuget/v/Enable.Extensions.Queuing.AzureStorage.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Enable.Extensions.Queuing.AzureStorage/)


## Queues vs. message buses

There are two types of messages, and therefore two types of messaging queues,
that we may want to leverage, depending on the intent of the publisher of a
message. The first type of messages are *commands* or *jobs* that must be
processed. The second are *events* that notify of something that has already
happened. In the former case, we would need an actor to action the command,
and we would want a single actor to do this, potentially reporting back the
outcome of the processing. In the latter case, we are simply notifying any
listeners of something that has already happened, and we don't have any
expectation of how that event is to be handled.

`Enable.Extensions.Queuing` provides the first of these types of messaging
queues. Use any of the queue implementations provided to queue up work items,
and have one out of any number of consumers process each individual message.

## Examples

The following example demonstrates posting a message to a queue and then
retrieving the same message. This uses a single application to both send and
receive messages. In production systems you'll more likely have (multiple)
different components publishing and consuming messages.

Here we're using the RabbitMQ queue provider. How we work with messages is the
same across any of the available queue implementations. The only differences
are in the options that are passed in when constructing the queue factory.

```csharp
using System;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Enable.Extensions.Queuing.RabbitMQ;

namespace QueuingSamples
{
    public class Program
    {
        public static void Main() => MainAsync().GetAwaiter().GetResult();

        public static async Task MainAsync()
        {
            var options = new RabbitMQQueueClientFactoryOptions
            {
                HostName = "localhost",
                Port = 5672,
                VirtualHost = "/"
                UserName = "guest",
                Password = "guest",
            };

            var queueFactory = new RabbitMQQueueClientFactory(options);

            var queueName = "your-queue-name";

            // Get a reference to the queue that you want to work with.
            // The queue will be automatically created if it doesn't exist.
            using (var queue = queueFactory.GetQueueReference(queueName))
            {
                // Add a new item to the queue.
                // Here we're using a `string`, but any type of message can
                // be used as long as it can be serialised to a byte array.
                await queue.EnqueueAsync("Hello, World!");

                // Retrieve our message from the queue.
                // We get this back as a type of `IQueueMessage` (note that
                // we could get `null` back here, if there are no messages
                // in the queue).
                var message = await queue.DequeueAsync();

                // The contents of the message that we sent are available
                // from `IQueueMessage.Body`. This property is our original
                // message, "Hello, World!", stored as a byte array. This
                // might not be that useful, so you can use the
                // `IQueueMessage.GetBody<T>` method to deserialise this back
                // to your original type.
                // In this case, we want to get our `string` back:
                var payload = message.GetBody<string>();

                // The following will print `Hello, World!`.
                Console.WriteLine(payload);

                // Now, do some clever work with the message.
                // Here's your chance to shine…

                // Finally, we acknowledge the message once we're done.
                // We can either "complete" the message, which means that
                // we've successfully processed it. This will remove the
                // message from our queue.
                await queue.CompleteAsync(message);

                // Or if something goes wrong and we can't process the
                // message, we can "abandon" it (but don't call both
                // `CompleteAsync` and `AbandonAsync`!).
                // await queue.AbandonAsync(message);
            }
        }
    }
}
```

[RabbitMQ]: https://www.rabbitmq.com/
[Azure Service Bus]: https://azure.microsoft.com/services/service-bus/
[Azure Storage]: https://azure.microsoft.com/services/storage/

[`Enable.Extensions.Queuing.Abstractions`]: https://www.nuget.org/packages/Enable.Extensions.Queuing.Abstractions/
[`Enable.Extensions.Queuing.InMemory`]: https://www.nuget.org/packages/Enable.Extensions.Queuing.InMemory/
[`Enable.Extensions.Queuing.RabbitMQ`]: https://www.nuget.org/packages/Enable.Extensions.Queuing.RabbitMQ/
[`Enable.Extensions.Queuing.AzureServiceBus`]: https://www.nuget.org/packages/Enable.Extensions.Queuing.AzureServiceBus/
[`Enable.Extensions.Queuing.AzureStorage`]: https://www.nuget.org/packages/Enable.Extensions.Queuing.AzureStorage/
