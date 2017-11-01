using Enable.Extensions.Queuing.Abstractions;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Enable.Extensions.Queuing.AzureStorage.Internal
{
    public class AzureStorageQueueMessage : IQueueMessage
    {
        private readonly CloudQueueMessage _message;

        public AzureStorageQueueMessage(CloudQueueMessage message)
        {
            _message = message;
        }

        public string MessageId => _message.Id;

        public string LeaseId => _message.PopReceipt;

        public uint DequeueCount => (uint)_message.DequeueCount;

        public byte[] Body => _message.AsBytes;
    }
}
