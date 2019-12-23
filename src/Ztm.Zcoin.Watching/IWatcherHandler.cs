using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Watching
{
    public interface IWatcherHandler<TContext, TWatch> where TWatch : Watch<TContext>
    {
        Task AddWatchesAsync(IEnumerable<TWatch> watches, CancellationToken cancellationToken);

        Task RemoveWatchAsync(TWatch watch, WatchRemoveReason reason, CancellationToken cancellationToken);
    }
}
