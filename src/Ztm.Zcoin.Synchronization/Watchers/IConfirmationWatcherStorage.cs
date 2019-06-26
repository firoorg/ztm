using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public interface IConfirmationWatcherStorage<T> : IWatcherStorage<T> where T : Watch
    {
        Task<IEnumerable<T>> GetWatchesAsync(CancellationToken cancellationToken);
    }
}
