using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public interface IWatcherStorage<T> where T : Watch
    {
        Task AddWatchesAsync(IEnumerable<T> watches, CancellationToken cancellationToken);

        Task RemoveWatchAsync(T watch, WatchRemoveReason reason, CancellationToken cancellationToken);
    }
}
