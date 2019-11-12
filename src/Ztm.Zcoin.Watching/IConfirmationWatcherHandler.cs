using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Watching
{
    public interface IConfirmationWatcherHandler<TWatch, TContext> : IWatcherHandler<TWatch, TContext>
        where TWatch : Watch<TContext>
    {
        Task<bool> ConfirmationUpdateAsync(
            TWatch watch,
            int confirmation,
            ConfirmationType type,
            CancellationToken cancellationToken);

        Task<IEnumerable<TWatch>> GetCurrentWatchesAsync(CancellationToken cancellationToken);
    }
}
