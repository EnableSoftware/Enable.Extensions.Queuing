using System;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal.ConnectionManagement
{
    internal class RabbitMQManagedConnection
    {
        public RabbitMQManagedConnection(IConnection connection)
        {
            Connection = connection;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public IConnection Connection { get; private set; }

        public int ReferenceCount { get; private set; } = 1;

        public bool ChannelAllocationReached { get; set; }

        public void DecrementReferenceCount()
        {
            ReferenceCount--;
        }

        public void IncrementReferenceCount()
        {
            ReferenceCount++;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (Connection != null && disposing)
            {
                Connection.Close();
                Connection.Dispose();

                Connection = null;
            }
        }
    }
}
