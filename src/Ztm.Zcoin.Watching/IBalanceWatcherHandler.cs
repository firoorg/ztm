using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public interface IBalanceWatcherHandler<TContext, TAmount> :
        IConfirmationWatcherHandler<TContext, BalanceWatch<TContext, TAmount>, BalanceConfirmation<TContext, TAmount>>
    {
        Task<IReadOnlyDictionary<BitcoinAddress, BalanceChange<TContext, TAmount>>> GetBalanceChangesAsync(
            Transaction tx,
            CancellationToken cancellationToken);
    }
}
