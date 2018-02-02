using Enable.Extensions.Queuing.Abstractions;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Enable.Extensions.Queuing.AzureStorage.Internal
{
    public class AzureStorageQueueMessage : BaseQueueMessage
    {
        private readonly CloudQueueMessage _message;

        public AzureStorageQueueMessage(CloudQueueMessage message)
        {
            _message = message;
        }

        public override string MessageId => _message.Id;

        public override string LeaseId => _message.PopReceipt;

        public override uint DequeueCount => (uint)_message.DequeueCount;

        public override byte[] Body => _message.AsBytes;
    }
}
