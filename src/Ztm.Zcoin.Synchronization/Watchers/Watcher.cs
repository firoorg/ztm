using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ServiceModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public abstract class Watcher<T> : BackgroundService, IBlockListener where T : Watch
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

        protected override Task OnStartAsync(CancellationToken cancellationToken)
        {
            return this.storage.StartAsync(cancellationToken);
        }

        protected override Task OnStopAsync(CancellationToken cancellationToken)
        {
            return this.storage.StopAsync(cancellationToken);
        }

        protected virtual Task RemoveWatchesAsync(
            IEnumerable<WatchToRemove<T>> watches,
            CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            if (!watches.Any())
            {
                throw new ArgumentException("The list is empty.", nameof(watches));
            }

            return this.storage.RemoveWatchesAsync(watches, cancellationToken);
        }

        async Task ExecuteWatchesAsync(
            ZcoinBlock block,
            int height,
            BlockEventType blockEventType,
            CancellationToken cancellationToken)
        {
            var removes = new Collection<WatchToRemove<T>>();

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
                    removes.Add(new WatchToRemove<T>(watch, removeReason));
                }
            }

            // Remove watches that need to remove.
            if (removes.Count > 0)
            {
                await RemoveWatchesAsync(removes, CancellationToken.None);
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
