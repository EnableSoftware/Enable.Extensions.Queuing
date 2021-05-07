using Enable.Extensions.Queuing.Abstractions;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Enable.Extensions.Queuing.AzureStorage.Internal
{
    public class AzureStorageQueueMessage : BaseQueueMessage
    {
        private readonly CloudQueueMessage _message;

        private string _leaseId;

        public AzureStorageQueueMessage(CloudQueueMessage message)
        {
            _message = message;
            _leaseId = _message.PopReceipt;
        }

        public override string MessageId => _message.Id;

        public override string LeaseId => _leaseId;

        public override string SessionId => null;

        public override uint DequeueCount => (uint)_message.DequeueCount;

        public override byte[] Body => _message.AsBytes;

        internal void UpdateLeaseId(string leaseId)
        {
            _leaseId = leaseId;
        }
    }
}
