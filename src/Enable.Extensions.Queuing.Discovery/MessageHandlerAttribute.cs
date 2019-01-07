using System;

namespace Enable.Extensions.Queuing.Discovery
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute
    {
        private readonly string _entityPath;

        public MessageHandlerAttribute(string entityPath)
        {
            if (string.IsNullOrWhiteSpace(entityPath))
            {
                throw new ArgumentException(nameof(entityPath));
            }

            _entityPath = entityPath;
        }

        public string EntityPath => _entityPath;
    }
}
