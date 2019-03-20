using System;
using System.Threading.Tasks;

namespace Enable.Extensions.Queuing.Abstractions
{
    public class MessageHandlerOptions
    {
        private int _maxConcurrentCalls = 1;

        public bool AutoComplete { get; set; } = true;

        public int MaxConcurrentCalls
        {
            get
            {
                return _maxConcurrentCalls;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        $"The specified value '{value}' is invalid. '{nameof(MaxConcurrentCalls)}' must be greater than zero.");
                }

                _maxConcurrentCalls = value;
            }
        }

        public int? PrefetchCount { get; set; }

        public Func<MessageHandlerExceptionContext, Task> ExceptionReceivedHandler { get; set; }
    }
}
