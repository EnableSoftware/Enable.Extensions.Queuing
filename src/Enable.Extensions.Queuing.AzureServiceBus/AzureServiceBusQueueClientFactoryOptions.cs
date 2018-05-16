using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Enable.Extensions.Queuing.AzureServiceBus
{
    public class AzureServiceBusQueueClientFactoryOptions
    {
        public string ConnectionString { get; set; }
        public int MaxConcurrentCalls { get; set; } = 1;
        public int PrefetchCount { get; set; }

        // TODO Replace this with an abstracted exception handler.
        public Func<ExceptionReceivedEventArgs, Task> ExceptionReceivedHandler { get; set; }
    }
}
