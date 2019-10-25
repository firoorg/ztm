using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public interface ITransactionConfirmationWatcherHandler<TContext> : IConfirmationWatcherHandler<TransactionWatch<TContext>, TContext>
    {
        Task<IEnumerable<TContext>> CreateContextsAsync(Transaction tx, CancellationToken cancellationToken);
    }
}
