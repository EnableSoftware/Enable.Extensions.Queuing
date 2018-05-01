using System;
using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Enable.Extensions.Queuing.AzureStorage.Internal
{
    internal class MessagePump
    {
        private readonly MessageReceiver _messageReceiver;

        private readonly Func<IQueueMessage, CancellationToken, Task> _onMessageCallback;

        private readonly CancellationToken _cancellationToken;

        private readonly SemaphoreSlim _semaphore;

        public MessagePump(
            Func<IQueueMessage, CancellationToken, Task> callback,
            MessageReceiver messageReceiver,
            CancellationToken cancellationToken)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (messageReceiver == null)
            {
                throw new ArgumentNullException(nameof(messageReceiver));
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            _onMessageCallback = callback;
            _messageReceiver = messageReceiver;
            _cancellationToken = cancellationToken;

            // TODO The number of concurrent calls should be configurable.
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public Task StartPumpAsync()
        {
            return Task.Run(() => MessagePumpTaskAsync());
        }

        private async Task MessagePumpTaskAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                IQueueMessage message = null;

                try
                {
                    await _semaphore.WaitAsync(_cancellationToken).ConfigureAwait(false);

                    message = await _messageReceiver(_cancellationToken).ConfigureAwait(false);

                    // TODO We need to back off when there are no messages in the queue.
                    if (message != null)
                    {
                        Task.Run(async () =>
                        {
                            await _onMessageCallback(message, _cancellationToken).ConfigureAwait(false);
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
