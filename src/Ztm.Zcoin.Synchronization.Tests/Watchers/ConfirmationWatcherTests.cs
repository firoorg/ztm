using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    public sealed class ConfirmationWatcherTests
    {
        readonly IConfirmationWatcherStorage<Watch> storage;
        readonly IBlocksStorage blocks;
        readonly TestConfirmationWatcher subject;

        public ConfirmationWatcherTests()
        {
            this.storage = Substitute.For<IConfirmationWatcherStorage<Watch>>();
            this.blocks = Substitute.For<IBlocksStorage>();
            this.subject = new TestConfirmationWatcher(this.storage, this.blocks);
            this.subject.Confirm = Substitute.For<Func<Watch, ConfirmationType, int, CancellationToken, bool>>();
            this.subject.CreateWatches = Substitute.For<Func<Block, int, CancellationToken, IEnumerable<Watch>>>();
        }

        [Fact]
        public void Constructor_WithNullBlocks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "blocks",
                () => new TestConfirmationWatcher(this.storage, null)
            );
        }

        [Fact]
        public async Task ExecuteAsync_HaveWatch_ShouldInvokeRequiredMethods()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch>());
            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.subject.Confirm(Arg.Any<Watch>(), Arg.Any<ConfirmationType>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Removing, cancellationSource.Token);

                // Assert.
                _ = this.storage.Received(2).GetWatchesAsync(cancellationSource.Token);
                _ = this.blocks.Received(2).GetAsync(block.GetHash(), CancellationToken.None);
                this.subject.Confirm.Received(1)(watch, ConfirmationType.Confirmed, 1, CancellationToken.None);
                this.subject.Confirm.Received(1)(watch, ConfirmationType.Unconfirming, 1, CancellationToken.None);
                _ = this.storage.Received(1).RemoveWatchAsync(watch, WatchRemoveReason.BlockRemoved, CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_WithWatchOnPreviousBlock_ShouldConfirmWithTwoConfirmation()
        {
            // Arrange.
            var block0 = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = Block.CreateBlock(ZcoinNetworks.Instance.Regtest);
            var watch = new Watch(block0.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch>());
            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));
            this.subject.Confirm(Arg.Any<Watch>(), Arg.Any<ConfirmationType>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block1, 1, BlockEventType.Added, cancellationSource.Token);
                await this.subject.ExecuteAsync(block1, 1, BlockEventType.Removing, cancellationSource.Token);

                // Assert.
                this.subject.Confirm.Received(1)(watch, ConfirmationType.Confirmed, 2, CancellationToken.None);
                this.subject.Confirm.Received(1)(watch, ConfirmationType.Unconfirming, 2, CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ConfirmAsyncReturnTrue_ShouldRemoveThatWatch()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch>());
            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.subject.Confirm(watch, ConfirmationType.Confirmed, 1, Arg.Any<CancellationToken>()).Returns(true);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                _ = this.storage.Received(1).RemoveWatchAsync(watch, WatchRemoveReason.Completed, CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ConfirmAsyncReturnFalse_ShouldKeepThatWatch()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.subject.Confirm(watch, ConfirmationType.Confirmed, 1, Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                _ = this.storage.Received(0).RemoveWatchAsync(Arg.Any<Watch>(), Arg.Any<WatchRemoveReason>(), Arg.Any<CancellationToken>());
            }
        }
    }
}
