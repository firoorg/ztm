using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    sealed class TestConfirmationWatcher : ConfirmationWatcher<Watch>
    {
        public TestConfirmationWatcher(IConfirmationWatcherHandler<Watch> handler, IBlocksStorage blocks)
            : base(handler, blocks)
        {
        }

        public Func<Block, int, CancellationToken, IEnumerable<Watch>> CreateWatches { get; set; }

        protected override Task<IEnumerable<Watch>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateWatches(block, height, cancellationToken));
        }
    }
}
