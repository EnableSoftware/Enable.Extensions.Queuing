namespace Enable.Extensions.Queuing.Abstractions
{
    internal class QueueMessage : BaseQueueMessage
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

        public override byte[] Body => _payload;

        public override uint DequeueCount => throw new System.NotImplementedException();

        public override string LeaseId => throw new System.NotImplementedException();

        public override string MessageId { get; }

        public override string SessionId => throw new System.NotImplementedException();
    }
}
