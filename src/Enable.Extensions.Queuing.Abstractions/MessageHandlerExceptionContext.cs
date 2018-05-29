using System;

namespace Enable.Extensions.Queuing.Abstractions
{
    public class MessageHandlerExceptionContext
    {
        public MessageHandlerExceptionContext(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
