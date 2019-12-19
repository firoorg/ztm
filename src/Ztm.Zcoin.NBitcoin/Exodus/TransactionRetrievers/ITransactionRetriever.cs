using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers
{
    public interface ITransactionRetriever
    {
        Task<IEnumerable<BalanceChange>> GetBalanceChangesAsync(Transaction transaction, CancellationToken cancellationToken);
    }
}