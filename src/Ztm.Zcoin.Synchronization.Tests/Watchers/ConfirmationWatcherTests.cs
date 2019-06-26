using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers
{
    public sealed class ConfirmationWatcherTests : IDisposable
    {
        readonly IConfirmationWatcherStorage<Watch> storage;
        readonly IBlocksStorage blocks;
        readonly TestConfirmationWatcher subject;

        public ConfirmationWatcherTests()
        {
            this.storage = Substitute.For<IConfirmationWatcherStorage<Watch>>();
            this.blocks = Substitute.For<IBlocksStorage>();
            this.subject = new TestConfirmationWatcher(this.storage, this.blocks);

            try
            {
                this.subject.Confirm = Substitute.For<Func<Watch, ConfirmationType, int, bool>>();
            }
            catch
            {
                this.subject.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.subject.Dispose();
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
        public async Task BlockAddedAsync_WithWatchOnPreviousBlock_ShouldConfirmWithTwoConfirmation()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.Consensus.ConsensusFactory.CreateBlock();
            var watch = new Watch(block0.GetHash());

            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));

            // Act.
            await this.subject.BlockAddedAsync(block1, 1);

            // Assert.
            this.subject.Confirm.Received(1)(watch, ConfirmationType.Confirmed, 2);
        }

        [Fact]
        public async Task BlockAddedAsync_ConfirmAsyncReturnTrue_ShouldRemoveThatWatch()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.subject.Confirm(watch, ConfirmationType.Confirmed, 1).Returns(true);

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            _ = this.storage.Received(1).RemoveWatchesAsync(
                Arg.Is<IEnumerable<WatchToRemove<Watch>>>(l => l.SequenceEqual(new[] { new WatchToRemove<Watch>(watch, WatchRemoveReason.Completed) })),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockAddedAsync_ConfirmAsyncReturnFalse_ShouldKeepThatWatch()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.subject.Confirm(watch, ConfirmationType.Confirmed, 1).Returns(false);

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            _ = this.storage.Received(0).RemoveWatchesAsync(
                Arg.Any<IEnumerable<WatchToRemove<Watch>>>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockRemovingAsync_WithWatchOnPreviousBlock_ShouldUnconfirmWithTwoConfirmation()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.Consensus.ConsensusFactory.CreateBlock();
            var watch = new Watch(block0.GetHash());

            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));
            this.subject.Confirm(watch, ConfirmationType.Unconfirming, 2).Returns(false);

            // Act.
            await this.subject.BlockRemovingAsync(block1, 1);

            // Assert.
            this.subject.Confirm.Received(1)(watch, ConfirmationType.Unconfirming, 2);

            _ = this.storage.Received(0).RemoveWatchesAsync(
                Arg.Any<IEnumerable<WatchToRemove<Watch>>>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockRemovingAsync_ConfirmAsyncReturnTrue_ShouldRemoveThatWatchWithCompletedReason()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.subject.Confirm(watch, ConfirmationType.Unconfirming, 1).Returns(true);

            // Act.
            await this.subject.BlockRemovingAsync(block, 0);

            // Assert.
            _ = this.storage.Received(1).RemoveWatchesAsync(
                Arg.Is<IEnumerable<WatchToRemove<Watch>>>(l => l.SequenceEqual(new[] { new WatchToRemove<Watch>(watch, WatchRemoveReason.Completed | WatchRemoveReason.BlockRemoved) })),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockRemovingAsync_ConfirmAsyncReturnFalse_ShouldNotRemoveThatWatchWithCompletedReason()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.storage.GetWatchesAsync(Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.blocks.GetAsync(block.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block, height: 0));
            this.subject.Confirm(watch, ConfirmationType.Unconfirming, 1).Returns(false);

            // Act.
            await this.subject.BlockRemovingAsync(block, 0);

            // Assert.
            _ = this.storage.Received(1).RemoveWatchesAsync(
                Arg.Is<IEnumerable<WatchToRemove<Watch>>>(l => l.SequenceEqual(new[] { new WatchToRemove<Watch>(watch, WatchRemoveReason.BlockRemoved) })),
                Arg.Any<CancellationToken>()
            );
        }
    }
}
