using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public abstract class Watcher<TContext, TWatch> where TWatch : Watch<TContext>
    {
        readonly IWatcherHandler<TContext, TWatch> handler;

        protected Watcher(IWatcherHandler<TContext, TWatch> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            this.handler = handler;
        }

        public async Task ExecuteAsync(
            Block block,
            int height,
            BlockEventType eventType,
            CancellationToken cancellationToken)
        {
            IEnumerable<TWatch> watches;

            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "The value is not valid height.");
            }

            // First, inspect block and create new watches.
            if (eventType == BlockEventType.Added)
            {
                watches = await CreateWatchesAsync(block, height, cancellationToken);

                if (watches.Any())
                {
                    await this.handler.AddWatchesAsync(watches, cancellationToken);
                }
            }

            // Load watches that match with the block and execute it.
            watches = await GetWatchesAsync(block, height, cancellationToken);

            if (!watches.Any())
            {
                return;
            }

            var completed = await ExecuteWatchesAsync(watches, block, height, eventType, cancellationToken);

            // First, remove completed watches.
            if (completed.Any())
            {
                await this.handler.RemoveCompletedWatchesAsync(completed, CancellationToken.None);
            }

            // Then remove watches that belong to the block is being removing.
            if (eventType == BlockEventType.Removing)
            {
                await this.handler.RemoveUncompletedWatchesAsync(block.GetHash(), CancellationToken.None);
            }
        }

        protected abstract Task<IEnumerable<TWatch>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken);

        protected abstract Task<ISet<TWatch>> ExecuteWatchesAsync(
            IEnumerable<TWatch> watches,
            Block block,
            int height,
            BlockEventType eventType,
            CancellationToken cancellationToken);

        protected abstract Task<IEnumerable<TWatch>> GetWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken);
    }
}
