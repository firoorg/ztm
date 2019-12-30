using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching.Tests
{
    sealed class FakeConfirmationWatcher : ConfirmationWatcher<object, Watch<object>, object>
    {
        public FakeConfirmationWatcher(
            IConfirmationWatcherHandler<object, Watch<object>, object> handler,
            IBlocksStorage blocks) : base(handler, blocks)
        {
            StubbedCreateWatchesAsync = new Mock<Func<Block, int, CancellationToken, Task<IEnumerable<Watch<object>>>>>();
            StubbedExecuteWatchesAsync = new Mock<Func<IEnumerable<Watch<object>>, Block, int, BlockEventType, CancellationToken, Task<ISet<Watch<object>>>>>();
        }

        public Mock<Func<Block, int, CancellationToken, Task<IEnumerable<Watch<object>>>>> StubbedCreateWatchesAsync { get; }

        public Mock<Func<IEnumerable<Watch<object>>, Block, int, BlockEventType, CancellationToken, Task<ISet<Watch<object>>>>> StubbedExecuteWatchesAsync { get; }

        public static new ConfirmationType GetConfirmationType(BlockEventType eventType)
        {
            return ConfirmationWatcher<object, Watch<object>, object>.GetConfirmationType(eventType);
        }

        public new Task<int> GetConfirmationAsync(
            Watch<object> watch,
            int currentHeight,
            CancellationToken cancellationToken)
        {
            return base.GetConfirmationAsync(watch, currentHeight, cancellationToken);
        }

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
    }
}
