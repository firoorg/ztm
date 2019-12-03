using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksRetriever : IDisposable
    {
        bool IsRunning { get; }

        Task<Task> StartAsync(IBlocksRetrieverHandler handler, CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
