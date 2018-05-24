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

                try
                {
                    await _semaphore.WaitAsync(_cancellationToken).ConfigureAwait(false);

                    message = await _queueClient.MessageReceiver(_cancellationToken).ConfigureAwait(false);

                    // TODO We need to back off when there are no messages in the queue.
                    if (message != null)
                    {
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
    }
}
