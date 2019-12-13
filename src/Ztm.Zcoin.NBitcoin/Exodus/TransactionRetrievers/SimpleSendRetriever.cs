using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers
{
    public sealed class SimpleSendRetriever : IExodusTransactionRetriever
    {
        public int SupportedId
        {
            get
            {
                return SimpleSendV0.StaticId;
            }
        }

        public Task<IEnumerable<BalanceChange>> GetBalanceChangesAsync(
            ExodusTransaction transaction, CancellationToken cancellationToken)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var simpleSend = (SimpleSendV0)transaction;

            var changes = new BalanceChange[]
            {
                new BalanceChange(simpleSend.Sender, PropertyAmount.Negate(simpleSend.Amount), simpleSend.Property),
                new BalanceChange(simpleSend.Receiver, simpleSend.Amount, simpleSend.Property),
            };

            return Task.FromResult(changes.AsEnumerable());
        }
    }
}