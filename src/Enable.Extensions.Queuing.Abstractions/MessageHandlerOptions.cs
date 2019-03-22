using System;
using System.Threading.Tasks;

namespace Enable.Extensions.Queuing.Abstractions
{
    public class MessageHandlerOptions
    {
        private int _maxConcurrentCalls = 1;
        private int _prefetchCount;

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

        public int PrefetchCount
        {
            get
            {
                return _prefetchCount;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        $"The specified value '{value}' is invalid. '{nameof(PrefetchCount)}' must be a positive integer.");
                }

                _prefetchCount = value;
            }
        }

        public Func<MessageHandlerExceptionContext, Task> ExceptionReceivedHandler { get; set; }
    }
}
