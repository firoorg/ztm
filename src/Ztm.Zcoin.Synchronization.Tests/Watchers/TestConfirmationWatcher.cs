using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;
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

        public IList<(Watch watch, WatchRemoveReason reason, CancellationToken cancellationToken)> RemovedWatches { get; } = new Collection<(Watch watch, WatchRemoveReason reason, CancellationToken cancellationToken)>();

        public Task BlockAddedAsync(ZcoinBlock block, int height, CancellationToken cancellationToken)
        {
            return ((IBlockListener)this).BlockAddedAsync(block, height, cancellationToken);
        }

        public Task BlockRemovingAsync(ZcoinBlock block, int height, CancellationToken cancellationToken)
        {
            return ((IBlockListener)this).BlockRemovingAsync(block, height, cancellationToken);
        }

        protected override Task<bool> ConfirmAsync(
            Watch watch,
            ConfirmationType type,
            int confirmation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Confirm(watch, type, confirmation, cancellationToken));
        }

        protected override Task<IEnumerable<Watch>> CreateWatchesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<Watch>());
        }

        protected override Task RemoveWatchAsync(Watch watch, WatchRemoveReason reason, CancellationToken cancellationToken)
        {
            RemovedWatches.Add((watch, reason, cancellationToken));
            return base.RemoveWatchAsync(watch, reason, cancellationToken);
        }
    }
}
