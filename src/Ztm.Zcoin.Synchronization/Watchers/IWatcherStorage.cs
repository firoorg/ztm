using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ServiceModel;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public interface IWatcherStorage<T> : IBackgroundService where T : Watch
    {
        Task AddWatchesAsync(IEnumerable<T> watches, CancellationToken cancellationToken);

        Task RemoveWatchesAsync(IEnumerable<WatchToRemove<T>> watches, CancellationToken cancellationToken);
    }
}
