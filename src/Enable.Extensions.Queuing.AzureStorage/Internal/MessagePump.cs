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

        private readonly CancellationToken _cancellationToken;

        private readonly SemaphoreSlim _semaphore;

        public MessagePump(
            Func<IQueueMessage, CancellationToken, Task> callback,
            AzureStorageQueueClient queueClient,
            CancellationToken cancellationToken)
        {
            _onMessageCallback = callback ?? throw new ArgumentNullException(nameof(callback));
            _queueClient = queueClient ?? throw new ArgumentNullException(nameof(queueClient));
            _cancellationToken = cancellationToken;

            // TODO The number of concurrent calls should be configurable.
            _semaphore = new SemaphoreSlim(1, 1);
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
                            catch
                            {
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
