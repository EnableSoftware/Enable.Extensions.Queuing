using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    public class AzureServiceBusQueueClientOptions
    {
        public int MaxConcurrentCalls { get; set; }
        public Func<ExceptionReceivedEventArgs, Task> ExceptionReceivedHandler { get; set; }
    }
}
