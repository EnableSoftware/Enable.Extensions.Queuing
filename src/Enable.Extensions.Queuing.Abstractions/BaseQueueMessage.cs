using System.Text;
using Newtonsoft.Json;

namespace Enable.Extensions.Queuing.Abstractions
{
    public abstract class BaseQueueMessage : IQueueMessage
    {
        public abstract string MessageId { get; }

        public abstract string LeaseId { get; }

        public abstract string SessionId { get; }

        public abstract uint DequeueCount { get; }

        public abstract byte[] Body { get; }

        public T GetBody<T>()
        {
            var payload = Body;

            var json = Encoding.UTF8.GetString(payload);

            var body = JsonConvert.DeserializeObject<T>(json);

            return body;
        }
    }
}
