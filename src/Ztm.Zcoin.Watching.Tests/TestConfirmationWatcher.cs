using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching.Tests
{
    sealed class TestConfirmationWatcher : ConfirmationWatcher<Watch<object>, object>
    {
        public TestConfirmationWatcher(
            IConfirmationWatcherHandler<Watch<object>, object> handler,
            IBlocksStorage blocks) : base(handler, blocks)
        {
        }

        public Func<Block, int, CancellationToken, IEnumerable<Watch<object>>> CreateWatches { get; set; }

        protected override Task<IEnumerable<Watch<object>>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateWatches(block, height, cancellationToken));
        }
    }
}
