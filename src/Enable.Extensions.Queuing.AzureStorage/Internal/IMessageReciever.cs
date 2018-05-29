using System.Threading;
using System.Threading.Tasks;
using Enable.Extensions.Queuing.Abstractions;

namespace Enable.Extensions.Queuing.AzureStorage.Internal
{
    public delegate Task<IQueueMessage> MessageReceiver(CancellationToken token);
}
