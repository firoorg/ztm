using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Watching.Tests
{
    sealed class TestWatcher : Watcher<Watch<object>, object>
    {
        public TestWatcher(IWatcherHandler<Watch<object>, object> handler) : base(handler)
        {
        }

        public Func<Block, int, CancellationToken, IEnumerable<Watch<object>>> CreateWatches { get; set; }

        public Func<Watch<object>, Block, int, BlockEventType, CancellationToken, bool> ExecuteMatchedWatch { get; set; }

        public Func<Block, int, CancellationToken, IEnumerable<Watch<object>>> GetWatches { get; set; }

        protected override Task<IEnumerable<Watch<object>>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateWatches(block, height, cancellationToken));
        }

        protected override Task<bool> ExecuteMatchedWatchAsync(
            Watch<object> watch,
            Block block,
            int height,
            BlockEventType blockEventType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ExecuteMatchedWatch(watch, block, height, blockEventType, cancellationToken));
        }

        protected override Task<IEnumerable<Watch<object>>> GetWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GetWatches(block, height, cancellationToken));
        }
    }
}
