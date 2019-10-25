using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public interface IConfirmationWatcherHandler<T> : IWatcherHandler<T> where T : Watch
    {
        Task<bool> ConfirmationUpdateAsync(
            T watch,
            int confirmation,
            ConfirmationType type,
            CancellationToken cancellationToken);

        Task<IEnumerable<T>> GetCurrentWatchesAsync(CancellationToken cancellationToken);
    }
}
