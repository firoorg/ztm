using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    sealed class TestWatcher : Watcher<Watch>
    {
        public TestWatcher(IWatcherHandler<Watch> handler) : base(handler)
        {
        }

        public Func<Block, int, CancellationToken, IEnumerable<Watch>> CreateWatches { get; set; }

        public Func<Watch, Block, int, BlockEventType, CancellationToken, bool> ExecuteMatchedWatch { get; set; }

        public Func<Block, int, CancellationToken, IEnumerable<Watch>> GetWatches { get; set; }

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
    }
}
