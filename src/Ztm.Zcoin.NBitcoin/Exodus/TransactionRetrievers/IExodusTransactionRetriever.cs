using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers
{
    public interface IExodusTransactionRetriever
    {
        int SupportedId { get; }
        Task<IEnumerable<BalanceChange>> GetBalanceChangesAsync(ExodusTransaction transaction, CancellationToken cancellationToken);
    }
}