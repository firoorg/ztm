using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NBitcoin;

namespace Ztm.Zcoin.Watching.Tests
{
    sealed class FakeWatcher : Watcher<object, Watch<object>>
    {
        public FakeWatcher(IWatcherHandler<object, Watch<object>> handler) : base(handler)
        {
            StubbedCreateWatchesAsync = new Mock<Func<Block, int, CancellationToken, Task<IEnumerable<Watch<object>>>>>();
            StubbedExecuteWatchesAsync = new Mock<Func<IEnumerable<Watch<object>>, Block, int, BlockEventType, CancellationToken, Task<ISet<Watch<object>>>>>();
            StubbedGetWatchesAsync = new Mock<Func<Block, int, CancellationToken, Task<IEnumerable<Watch<object>>>>>();
        }

        public Mock<Func<Block, int, CancellationToken, Task<IEnumerable<Watch<object>>>>> StubbedCreateWatchesAsync { get; }

        public Mock<Func<IEnumerable<Watch<object>>, Block, int, BlockEventType, CancellationToken, Task<ISet<Watch<object>>>>> StubbedExecuteWatchesAsync { get; }

        public Mock<Func<Block, int, CancellationToken, Task<IEnumerable<Watch<object>>>>> StubbedGetWatchesAsync { get; }

        protected override Task<IEnumerable<Watch<object>>> CreateWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return StubbedCreateWatchesAsync.Object(block, height, cancellationToken);
        }

        protected override Task<ISet<Watch<object>>> ExecuteWatchesAsync(
            IEnumerable<Watch<object>> watches,
            Block block,
            int height,
            BlockEventType eventType,
            CancellationToken cancellationToken)
        {
            return StubbedExecuteWatchesAsync.Object(watches, block, height, eventType, cancellationToken);
        }

        protected override Task<IEnumerable<Watch<object>>> GetWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return StubbedGetWatchesAsync.Object(block, height, cancellationToken);
        }
    }
}
