using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    class TestWatcher : Watcher<Watch>
    {
        public TestWatcher(IWatcherStorage<Watch> storage) : base(storage)
        {
        }

        public Func<Block, int, CancellationToken, IEnumerable<Watch>> CreateWatches { get; set; }

        public Func<Watch, Block, int, BlockEventType, CancellationToken, bool> ExecuteMatchedWatch { get; set; }

        public Func<Block, int, CancellationToken, IEnumerable<Watch>> GetWatches { get; set; }

        public IList<(Watch watch, WatchRemoveReason reason, CancellationToken cancellationToken)> RemovedWatches { get; } = new Collection<(Watch watch, WatchRemoveReason reason, CancellationToken cancellationToken)>();

        public Task BlockAddedAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return ((IBlockListener)this).BlockAddedAsync(block, height, cancellationToken);
        }

        public Task BlockRemovingAsync(Block block, int height, CancellationToken cancellationToken)
        {
            return ((IBlockListener)this).BlockRemovingAsync(block, height, cancellationToken);
        }

        protected override Task<IEnumerable<Watch>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateWatches(block, height, cancellationToken));
        }

        protected override Task<bool> ExecuteMatchedWatchAsync(
            Watch watch,
            Block block,
            int height,
            BlockEventType blockEventType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ExecuteMatchedWatch(watch, block, height, blockEventType, cancellationToken));
        }

        protected override Task<IEnumerable<Watch>> GetWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GetWatches(block, height, cancellationToken));
        }

        protected override Task RemoveWatchAsync(Watch watch, WatchRemoveReason reason, CancellationToken cancellationToken)
        {
            RemovedWatches.Add((watch, reason, cancellationToken));
            return base.RemoveWatchAsync(watch, reason, cancellationToken);
        }
    }
}
