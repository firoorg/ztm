using System;
using System.Collections.Generic;
using System.Linq;
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

        public Func<Watch, ConfirmationType, int, bool> Confirm { get; set; }

        public Task BlockAddedAsync(ZcoinBlock block, int height)
        {
            return ((IBlockListener)this).BlockAddedAsync(block, height);
        }

        public Task BlockRemovingAsync(ZcoinBlock block, int height)
        {
            return ((IBlockListener)this).BlockRemovingAsync(block, height);
        }

        protected override Task<bool> ConfirmAsync(Watch watch, ConfirmationType type, int confirmation)
        {
            return Task.FromResult(Confirm(watch, type, confirmation));
        }

        protected override Task<IEnumerable<Watch>> CreateWatchesAsync(ZcoinBlock block, int height)
        {
            return Task.FromResult(Enumerable.Empty<Watch>());
        }
    }
}
