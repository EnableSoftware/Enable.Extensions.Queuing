namespace Enable.Extensions.Queuing.Abstractions
{
    internal class QueueMessage : BaseQueueMessage
    {
        private readonly byte[] _payload;
        private readonly string _sessionId;

        public QueueMessage()
        {
            _payload = new byte[0];
            _sessionId = null;
        }

        public QueueMessage(byte[] payload)
        {
            _payload = payload;
            _sessionId = null;
        }

        public QueueMessage(byte[] payload, string sessionId)
        {
            _payload = payload;
            _sessionId = sessionId;
        }

        public override byte[] Body => _payload;

        public override uint DequeueCount => throw new System.NotImplementedException();

        public override string LeaseId => throw new System.NotImplementedException();

        public override string MessageId { get; }

        public override string SessionId => _sessionId;
    }
}
