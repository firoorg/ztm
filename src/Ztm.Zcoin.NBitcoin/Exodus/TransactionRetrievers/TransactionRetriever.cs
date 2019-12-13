using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers
{
    public class TransactionRetriever : ITransactionRetriever
    {
        readonly IDictionary<int, IExodusTransactionRetriever> transactionRetrievers;

        public TransactionRetriever(IEnumerable<IExodusTransactionRetriever> transactionRetrievers)
        {
            if (transactionRetrievers == null)
            {
                throw new ArgumentNullException(nameof(transactionRetrievers));
            }

            this.transactionRetrievers = transactionRetrievers.ToDictionary(i => i.SupportedId);
        }

        public Task<IEnumerable<BalanceChange>> GetBalanceChangesAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var ex = transaction.GetExodusTransaction();
            if (ex == null)
            {
                return Task.FromResult<IEnumerable<BalanceChange>>(null);
            }

            if (!this.transactionRetrievers.TryGetValue(ex.Id, out var retriever))
            {
                throw new TransactionException("The Exodus transaction type is not supported.");
            }

            return retriever.GetBalanceChangesAsync(ex, cancellationToken);
        }
    }
}