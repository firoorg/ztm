using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    class TestConfirmationWatcher : ConfirmationWatcher<Watch>
    {
        public TestConfirmationWatcher(IConfirmationWatcherStorage<Watch> storage, IBlocksStorage blocks)
            : base(storage, blocks)
        {
        }

        public Func<Watch, ConfirmationType, int, CancellationToken, bool> Confirm { get; set; }

        public Func<Block, int, CancellationToken, IEnumerable<Watch>> CreateWatches { get; set; }

        protected override Task<bool> ConfirmAsync(
            Watch watch,
            ConfirmationType type,
            int confirmation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Confirm(watch, type, confirmation, cancellationToken));
        }

        protected override Task<IEnumerable<Watch>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateWatches(block, height, cancellationToken));
        }
    }
}
