using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public abstract class Watcher<T> where T : Watch
    {
        readonly IWatcherHandler<T> handler;

        protected Watcher(IWatcherHandler<T> handler)
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
            IEnumerable<T> watches;

            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
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
            foreach (var watch in await GetWatchesAsync(block, height, cancellationToken))
            {
                var success = await ExecuteMatchedWatchAsync(
                    watch,
                    block,
                    height,
                    eventType,
                    CancellationToken.None
                );

                // Determine if we need to remove watch.
                var removeReason = WatchRemoveReason.None;

                if (success)
                {
                    removeReason |= WatchRemoveReason.Completed;
                }

                if (eventType == BlockEventType.Removing && watch.StartBlock == block.GetHash())
                {
                    removeReason |= WatchRemoveReason.BlockRemoved;
                }

                if (removeReason != WatchRemoveReason.None)
                {
                    await this.handler.RemoveWatchAsync(watch, removeReason, CancellationToken.None);
                }
            }
        }

        protected abstract Task<IEnumerable<T>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken);

        protected abstract Task<bool> ExecuteMatchedWatchAsync(
            T watch,
            Block block,
            int height,
            BlockEventType blockEventType,
            CancellationToken cancellationToken);

        protected abstract Task<IEnumerable<T>> GetWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken);
    }
}
