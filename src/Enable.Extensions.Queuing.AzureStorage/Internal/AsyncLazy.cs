using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Enable.Extensions.Queuing.AzureStorage.Internal
{
    /// <summary>
    /// Provides support for asynchronous lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type of object that is being lazily initialized.</typeparam>
    /// <remarks>
    /// See https://blogs.msdn.microsoft.com/pfxteam/2011/01/15/asynclazyt/ for
    /// more information.
    /// </remarks>
    internal class AsyncLazy<T> : Lazy<Task<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}" /> class.
        /// </summary>
        /// <param name="valueFactory">
        /// The delegate that is invoked in order to produce the value when it is needed.
        /// </param>
        /// <remarks>
        /// The delegate <paramref name="valueFactory"/> is invoked on a background thread.
        /// </remarks>
        public AsyncLazy(Func<T> valueFactory)
            : base(() => Task.Factory.StartNew(valueFactory))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class.
        /// </summary>
        /// <param name="valueFactory">
        /// The asynchronous delegate that is invoked in order to produce the value when it is needed.
        /// </param>
        /// <remarks>
        /// The delegate <paramref name="valueFactory"/> is invoked on a background thread.
        /// </remarks>
        public AsyncLazy(Func<Task<T>> taskFactory)
            : base(() => Task.Run(() => taskFactory()))
        {
        }

        /// <summary>
        /// Support awaiting on instances of the class.
        /// </summary>
        /// <remarks>
        /// This method is intended for compiler use rather than for use in application code.
        /// </remarks>
        public TaskAwaiter<T> GetAwaiter()
        {
            return Value.GetAwaiter();
        }
    }
}
