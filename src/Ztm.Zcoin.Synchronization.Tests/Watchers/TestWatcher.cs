using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ztm.ObjectModel;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    class TestWatcher : Watcher<Watch>
    {
        public TestWatcher(IWatcherStorage<Watch> storage) : base(storage)
        {
            NewWatches = new Dictionary<ZcoinBlock, ICollection<Watch>>(ReferenceEqualityComparer<ZcoinBlock>.Default);
            Watches = new Dictionary<ZcoinBlock, ICollection<Watch>>(ReferenceEqualityComparer<ZcoinBlock>.Default);
        }

        public Func<Watch, ZcoinBlock, int, BlockEventType, bool> ExecuteMatchedWatch { get; set; }

        public IDictionary<ZcoinBlock, ICollection<Watch>> NewWatches { get; }

        public IDictionary<ZcoinBlock, ICollection<Watch>> Watches { get; }

        public Task BlockAddedAsync(ZcoinBlock block, int height)
        {
            return ((IBlockListener)this).BlockAddedAsync(block, height);
        }

        public Task BlockRemovingAsync(ZcoinBlock block, int height)
        {
            return ((IBlockListener)this).BlockRemovingAsync(block, height);
        }

        protected override Task<IEnumerable<Watch>> CreateWatchesAsync(ZcoinBlock block, int height)
        {
            if (NewWatches.TryGetValue(block, out var watches))
            {
                return Task.FromResult<IEnumerable<Watch>>(watches);
            }

            return Task.FromResult(Enumerable.Empty<Watch>());
        }

        protected override Task<bool> ExecuteMatchedWatchAsync(
            Watch watch,
            ZcoinBlock block,
            int height,
            BlockEventType blockEventType)
        {
            return Task.FromResult(ExecuteMatchedWatch(watch, block, height, blockEventType));
        }

        protected override Task<IEnumerable<Watch>> GetWatchesAsync(ZcoinBlock block, int height)
        {
            if (Watches.TryGetValue(block, out var watches))
            {
                return Task.FromResult<IEnumerable<Watch>>(watches);
            }

            return Task.FromResult(Enumerable.Empty<Watch>());
        }
    }
}
