using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    public class AzureServiceBusQueueMessage : IQueueMessage
    {
        private readonly Message _message;

        public AzureServiceBusQueueMessage(Message message)
        {
            _message = message;
        }

        public string MessageId => _message.MessageId;

        public byte[] Body => _message.Body;

        public uint DequeueCount => (uint)_message.SystemProperties.DeliveryCount;

        public string LeaseId => _message.SystemProperties.LockToken;
    }
}
