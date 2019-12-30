using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public interface ITransactionConfirmationWatcherHandler<TContext> :
        IConfirmationWatcherHandler<TContext, TransactionWatch<TContext>, TransactionWatch<TContext>>
    {
        Task<IEnumerable<TContext>> CreateContextsAsync(Transaction tx, CancellationToken cancellationToken);
    }
}
