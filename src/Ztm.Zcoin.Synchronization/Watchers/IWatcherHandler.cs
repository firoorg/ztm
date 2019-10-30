using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public interface IWatcherHandler<TWatch, TContext> where TWatch : Watch<TContext>
    {
        Task AddWatchesAsync(IEnumerable<TWatch> watches, CancellationToken cancellationToken);

        Task RemoveWatchAsync(TWatch watch, WatchRemoveReason reason, CancellationToken cancellationToken);
    }
}
