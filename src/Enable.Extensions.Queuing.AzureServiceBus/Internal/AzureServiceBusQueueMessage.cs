using Enable.Extensions.Queuing.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Enable.Extensions.Queuing.AzureServiceBus.Internal
{
    public class AzureServiceBusQueueMessage : BaseQueueMessage
    {
        private readonly Message _message;

        public AzureServiceBusQueueMessage(Message message)
        {
            _message = message;
        }

        public override string MessageId => _message.MessageId;

        public override byte[] Body => _message.Body;

        public override uint DequeueCount => (uint)_message.SystemProperties.DeliveryCount;

        public override string LeaseId => _message.SystemProperties.LockToken;
    }
}
