using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public abstract class Watcher<T> : IBlockListener where T : Watch
    {
        readonly IWatcherStorage<T> storage;

        protected Watcher(IWatcherStorage<T> storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            this.storage = storage;
        }

        protected abstract Task<IEnumerable<T>> CreateWatchesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken);

        protected abstract Task<bool> ExecuteMatchedWatchAsync(
            T watch,
            ZcoinBlock block,
            int height,
            BlockEventType blockEventType,
            CancellationToken cancellationToken);

        protected abstract Task<IEnumerable<T>> GetWatchesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken);

        protected virtual Task RemoveWatchAsync(T watch, WatchRemoveReason reason, CancellationToken cancellationToken)
        {
            if (watch == null)
            {
                throw new ArgumentNullException(nameof(watch));
            }

            return this.storage.RemoveWatchAsync(watch, cancellationToken);
        }

        async Task ExecuteWatchesAsync(
            ZcoinBlock block,
            int height,
            BlockEventType blockEventType,
            CancellationToken cancellationToken)
        {
            // Load watches that match with the block and execute it.
            foreach (var watch in await GetWatchesAsync(block, height, cancellationToken))
            {
                var success = await ExecuteMatchedWatchAsync(
                    watch,
                    block,
                    height,
                    blockEventType,
                    CancellationToken.None
                );

                // Determine if we need to remove watch.
                var removeReason = WatchRemoveReason.None;

                if (success)
                {
                    removeReason |= WatchRemoveReason.Completed;
                }

                if (blockEventType == BlockEventType.Removing && watch.StartBlock == block.GetHash())
                {
                    removeReason |= WatchRemoveReason.BlockRemoved;
                }

                if (removeReason != WatchRemoveReason.None)
                {
                    await RemoveWatchAsync(watch, removeReason, CancellationToken.None);
                }
            }
        }

        async Task IBlockListener.BlockAddedAsync(ZcoinBlock block, int height, CancellationToken cancellationToken)
        {
            var watches = await CreateWatchesAsync(block, height, cancellationToken);

            if (watches.Any())
            {
                await this.storage.AddWatchesAsync(watches, CancellationToken.None);
            }

            await ExecuteWatchesAsync(block, height, BlockEventType.Added, cancellationToken);
        }

        async Task IBlockListener.BlockRemovingAsync(ZcoinBlock block, int height, CancellationToken cancellationToken)
        {
            await ExecuteWatchesAsync(block, height, BlockEventType.Removing, cancellationToken);
        }
    }
}
