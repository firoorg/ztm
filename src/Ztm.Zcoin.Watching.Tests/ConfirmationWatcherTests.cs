using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class ConfirmationWatcherTests
    {
        readonly IConfirmationWatcherHandler<Watch<object>, object> handler;
        readonly IBlocksStorage blocks;
        readonly TestConfirmationWatcher subject;

        public ConfirmationWatcherTests()
        {
            this.handler = Substitute.For<IConfirmationWatcherHandler<Watch<object>, object>>();
            this.blocks = Substitute.For<IBlocksStorage>();
            this.subject = new TestConfirmationWatcher(this.handler, this.blocks);
            this.subject.CreateWatches = Substitute.For<Func<Block, int, CancellationToken, IEnumerable<Watch<object>>>>();
        }

        [Fact]
        public void Constructor_WithNullBlocks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "blocks",
                () => new TestConfirmationWatcher(this.handler, null)
            );
        }

        [Fact]
        public async Task ExecuteAsync_HaveWatch_ShouldInvokeRequiredMethods()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch<object>(null, block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.handler.GetCurrentWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.handler.ConfirmationUpdateAsync(Arg.Any<Watch<object>>(), Arg.Any<int>(), Arg.Any<ConfirmationType>(), Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Removing, cancellationSource.Token);

                // Assert.
                _ = this.handler.Received(2).GetCurrentWatchesAsync(cancellationSource.Token);
                _ = this.blocks.Received(2).GetAsync(block.GetHash(), CancellationToken.None);
                _ = this.handler.Received(1).ConfirmationUpdateAsync(watch, 1, ConfirmationType.Confirmed, CancellationToken.None);
                _ = this.handler.Received(1).ConfirmationUpdateAsync(watch, 1, ConfirmationType.Unconfirming, CancellationToken.None);
                _ = this.handler.Received(1).RemoveWatchAsync(watch, WatchRemoveReason.BlockRemoved, CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_WithWatchOnPreviousBlock_ShouldConfirmWithTwoConfirmation()
        {
            // Arrange.
            var block0 = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            var watch = new Watch<object>(null, block0.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.handler.GetCurrentWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));
            this.handler.ConfirmationUpdateAsync(Arg.Any<Watch<object>>(), Arg.Any<int>(), Arg.Any<ConfirmationType>(), Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block1, 1, BlockEventType.Added, cancellationSource.Token);
                await this.subject.ExecuteAsync(block1, 1, BlockEventType.Removing, cancellationSource.Token);

                // Assert.
                _ = this.handler.Received(1).ConfirmationUpdateAsync(watch, 2, ConfirmationType.Confirmed, CancellationToken.None);
                _ = this.handler.Received(1).ConfirmationUpdateAsync(watch, 2, ConfirmationType.Unconfirming, CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ConfirmationUpdateAsyncReturnTrue_ShouldInvokeRemoveWatchAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch<object>(null, block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.handler.GetCurrentWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.handler.ConfirmationUpdateAsync(watch, 1, ConfirmationType.Confirmed, Arg.Any<CancellationToken>()).Returns(true);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                _ = this.handler.Received(1).RemoveWatchAsync(watch, WatchRemoveReason.Completed, CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ConfirmationUpdateAsyncReturnFalse_ShouldNotInvokeRemoveWatchAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch<object>(null, block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
            this.handler.GetCurrentWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.handler.ConfirmationUpdateAsync(watch, 1, ConfirmationType.Confirmed, Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                _ = this.handler.Received(0).RemoveWatchAsync(Arg.Any<Watch<object>>(), Arg.Any<WatchRemoveReason>(), Arg.Any<CancellationToken>());
            }
        }
    }
}
