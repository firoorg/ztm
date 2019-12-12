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
        readonly IDictionary<Type, IExodusTransactionRetriever> transactionInterpreter;

        public TransactionRetriever(IEnumerable<IExodusTransactionRetriever> transactionInterpreters)
        {
            if (transactionInterpreters == null)
            {
                throw new ArgumentNullException(nameof(transactionInterpreters));
            }

            this.transactionInterpreter = transactionInterpreters.ToDictionary(i => i.SupportType);
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
                throw new ArgumentException("The transaction does not contain exodus data.", nameof(transaction));
            }

            if (!this.transactionInterpreter.TryGetValue(ex.GetType(), out var interpreter))
            {
                throw new TransactionFieldException(
                    TransactionFieldException.TypeField, "The value is unknown transaction type.");
            }

            return interpreter.GetBalanceChangesAsync(ex, cancellationToken);
        }
    }
}