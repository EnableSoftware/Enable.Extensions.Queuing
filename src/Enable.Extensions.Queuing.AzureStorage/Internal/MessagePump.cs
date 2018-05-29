using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.AzureStorage.Internal
{
    internal class MessagePump
    {
        private readonly AzureStorageQueueClient _queueClient;

        private readonly Func<IQueueMessage, CancellationToken, Task> _onMessageCallback;

        private readonly MessageHandlerOptions _messageHandlerOptions;

        private readonly CancellationToken _cancellationToken;

        private readonly SemaphoreSlim _semaphore;

        private readonly ExponentialBackoffRetryStrategy _retryStrategy;

        public MessagePump(
            AzureStorageQueueClient queueClient,
            Func<IQueueMessage, CancellationToken, Task> callback,
            MessageHandlerOptions messageHandlerOptions,
            CancellationToken cancellationToken)
        {
            _queueClient = queueClient ?? throw new ArgumentNullException(nameof(queueClient));
            _messageHandlerOptions = messageHandlerOptions ?? throw new ArgumentNullException(nameof(messageHandlerOptions));
            _onMessageCallback = callback ?? throw new ArgumentNullException(nameof(callback));
            _cancellationToken = cancellationToken;

            _semaphore = new SemaphoreSlim(
                messageHandlerOptions.MaxConcurrentCalls,
                messageHandlerOptions.MaxConcurrentCalls);

            _retryStrategy = new ExponentialBackoffRetryStrategy(
                delay: TimeSpan.FromSeconds(10),
                maximumDelay: TimeSpan.FromSeconds(30));
        }

        public Task StartPumpAsync()
        {
            Task.Run(MessagePumpTaskAsync).Ignore();
            return Task.CompletedTask;
        }

        private async Task MessagePumpTaskAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                IQueueMessage message = null;

                var pollingRetryCount = 0;

                try
                {
                    await _semaphore.WaitAsync(_cancellationToken).ConfigureAwait(false);

                    message = await _queueClient.MessageReceiver(_cancellationToken).ConfigureAwait(false);

                    if (message != null)
                    {
                        pollingRetryCount = 0;

                        Task.Run(async () =>
                        {
                            try
                            {
                                await _onMessageCallback(message, _cancellationToken).ConfigureAwait(false);
                                await _queueClient.CompleteAsync(message, _cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    var exceptionHandler = _messageHandlerOptions?.ExceptionReceivedHandler;

                                    if (exceptionHandler != null)
                                    {
                                        var context = new MessageHandlerExceptionContext(ex);

                                        await exceptionHandler(context).ConfigureAwait(false);
                                    }
                                }
                                catch
                                {
                                }

                                await _queueClient.AbandonAsync(message, _cancellationToken).ConfigureAwait(false);

                                throw;
                            }
                        })
                        .Ignore();
                    }
                    else
                    {
                        // Back off from polling when there are no messages in the queue.
                        var interval = _retryStrategy.GetRetryInterval(++pollingRetryCount);

                        await Task.Delay(interval);
                    }
                }
                finally
                {
                    if (message == null)
                    {
                        _semaphore.Release();
                    }
                }
            }
        }

        private struct ExponentialBackoffRetryStrategy
        {
            private readonly int _delayMilliseconds;
            private readonly int _maximumDelayMilliseconds;

            public ExponentialBackoffRetryStrategy(
                TimeSpan delay,
                TimeSpan maximumDelay)
            {
                _delayMilliseconds = (int)delay.TotalMilliseconds;
                _maximumDelayMilliseconds = (int)maximumDelay.TotalMilliseconds;
            }

            public TimeSpan GetRetryInterval(int retryCount)
            {
                retryCount = Math.Min(30, retryCount);

                var delay = Math.Min(
                    _delayMilliseconds * (Math.Pow(2, retryCount - 1) - 1) / 2,
                    _maximumDelayMilliseconds);

                var interval = TimeSpan.FromMilliseconds(delay);

                return interval;
            }
        }
    }
}
