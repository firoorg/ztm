using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public sealed class TransactionConfirmationWatcher<TContext> : ConfirmationWatcher<TransactionWatch<TContext>, TContext>
    {
        readonly ITransactionConfirmationWatcherHandler<TContext> handler;

        public TransactionConfirmationWatcher(ITransactionConfirmationWatcherHandler<TContext> handler, IBlocksStorage blocks)
            : base(handler, blocks)
        {
            this.handler = handler;
        }

        protected override async Task<IEnumerable<TransactionWatch<TContext>>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            var watches = new Collection<TransactionWatch<TContext>>();

            foreach (var tx in block.Transactions)
            {
                var contexts = await this.handler.CreateContextsAsync(tx, cancellationToken);

                foreach (var context in contexts) {
                    watches.Add(new TransactionWatch<TContext>(context, block.GetHash(), tx.GetHash()));
                }
            }

            return watches;
        }
    }
}
