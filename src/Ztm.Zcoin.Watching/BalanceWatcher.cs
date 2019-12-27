using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching
{
    public sealed class BalanceWatcher<TContext, TAmount> :
        ConfirmationWatcher<TContext, BalanceWatch<TContext, TAmount>, BalanceConfirmation<TContext, TAmount>>
    {
        readonly IBalanceWatcherHandler<TContext, TAmount> handler;

        public BalanceWatcher(IBalanceWatcherHandler<TContext, TAmount> handler, IBlocksStorage blocks)
            : base(handler, blocks)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            this.handler = handler;
        }

        protected override async Task<IEnumerable<BalanceWatch<TContext, TAmount>>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            var watches = new Collection<BalanceWatch<TContext, TAmount>>();

            foreach (var tx in block.Transactions)
            {
                var changes = await this.handler.GetBalanceChangesAsync(tx, cancellationToken);

                foreach (var entry in changes)
                {
                    var address = entry.Key;
                    var change = entry.Value;

                    var watch = new BalanceWatch<TContext, TAmount>(
                        change.Context,
                        block.GetHash(),
                        tx.GetHash(),
                        address,
                        change.Amount
                    );

                    watches.Add(watch);
                }
            }

            return watches;
        }

        protected override async Task<ISet<BalanceWatch<TContext, TAmount>>> ExecuteWatchesAsync(
            IEnumerable<BalanceWatch<TContext, TAmount>> watches,
            Block block,
            int height,
            BlockEventType eventType,
            CancellationToken cancellationToken)
        {
            var confirmationType = GetConfirmationType(eventType);
            var completed = new HashSet<BalanceWatch<TContext, TAmount>>();

            foreach (var group in watches.GroupBy(w => w.Address))
            {
                // Get confirmation number for each change.
                var changes = new Collection<ConfirmedBalanceChange<TContext, TAmount>>();

                foreach (var watch in group) // lgtm[cs/linq/missed-select]
                {
                    var change = new ConfirmedBalanceChange<TContext, TAmount>(
                        watch.Context,
                        watch.BalanceChange,
                        await GetConfirmationAsync(watch, height, CancellationToken.None)
                    );

                    changes.Add(change);
                }

                // Invoke handler.
                var confirm = new BalanceConfirmation<TContext, TAmount>(group.Key, changes);
                var confirmationCount = changes.Min(c => c.Confirmation);

                var success = await this.handler.ConfirmationUpdateAsync(
                    confirm,
                    confirmationCount,
                    confirmationType,
                    CancellationToken.None
                );

                if (success)
                {
                    foreach (var watch in group)
                    {
                        if (!completed.Add(watch))
                        {
                            throw new ArgumentException("The collection contains duplicated items.", nameof(watches));
                        }
                    }
                }
            }

            return completed;
        }
    }
}
