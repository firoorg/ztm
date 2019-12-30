using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching
{
    public sealed class TransactionConfirmationWatcher<TContext> :
        ConfirmationWatcher<TContext, TransactionWatch<TContext>, TransactionWatch<TContext>>
    {
        readonly ITransactionConfirmationWatcherHandler<TContext> handler;

        public TransactionConfirmationWatcher(
            ITransactionConfirmationWatcherHandler<TContext> handler,
            IBlocksStorage blocks) : base(handler, blocks)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

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

        protected override async Task<ISet<TransactionWatch<TContext>>> ExecuteWatchesAsync(
            IEnumerable<TransactionWatch<TContext>> watches,
            Block block,
            int height,
            BlockEventType eventType,
            CancellationToken cancellationToken)
        {
            var confirmationType = GetConfirmationType(eventType);
            var completed = new HashSet<TransactionWatch<TContext>>();

            foreach (var watch in watches)
            {
                var confirmation = await GetConfirmationAsync(watch, height, CancellationToken.None);

                var success = await this.handler.ConfirmationUpdateAsync(
                    watch,
                    confirmation,
                    confirmationType,
                    CancellationToken.None
                );

                if (success && !completed.Add(watch))
                {
                    throw new ArgumentException("The collection contains duplicated items.", nameof(watches));
                }
            }

            return completed;
        }
    }
}
