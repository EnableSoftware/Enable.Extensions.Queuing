using System;
using RabbitMQ.Client;

namespace Enable.Extensions.Queuing.RabbitMQ.Internal
{
    internal class RabbitMQManagedConnection
    {
        public RabbitMQManagedConnection(IConnection connection)
        {
            Connection = connection;
        }

        public IConnection Connection { get; }

        public int ReferenceCount { get;  private set; }

        public bool Disposed { get; private set; }

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
            if (!Disposed)
            {
                if (disposing)
                {
                    Connection.Close();
                    Connection.Dispose();
                }

                Disposed = true;
            }
        }
    }
}
