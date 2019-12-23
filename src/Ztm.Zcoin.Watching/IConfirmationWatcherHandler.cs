using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Watching
{
    public interface IConfirmationWatcherHandler<TContext, TWatch, TConfirm> : IWatcherHandler<TContext, TWatch>
        where TWatch : Watch<TContext>
    {
        Task<bool> ConfirmationUpdateAsync(
            TConfirm confirm,
            int confirmation,
            ConfirmationType type,
            CancellationToken cancellationToken);

        Task<IEnumerable<TWatch>> GetCurrentWatchesAsync(CancellationToken cancellationToken);
    }
}
