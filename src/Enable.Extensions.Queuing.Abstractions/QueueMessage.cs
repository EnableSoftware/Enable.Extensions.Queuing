namespace Enable.Extensions.Queuing.Abstractions
{
    internal class QueueMessage : IQueueMessage
    {
        private readonly byte[] _payload;

        public QueueMessage()
        {
            _payload = new byte[0];
        }

        public QueueMessage(byte[] payload)
        {
            _payload = payload;
        }

        public byte[] Body => _payload;

        public uint DequeueCount => throw new System.NotImplementedException();
        
        public string LeaseId => throw new System.NotImplementedException();

        public string MessageId { get; }
    }
}
