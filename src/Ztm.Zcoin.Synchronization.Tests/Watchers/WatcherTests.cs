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
    public sealed class WatcherTests : IDisposable
    {
        readonly IWatcherStorage<Watch> storage;
        readonly TestWatcher subject;

        public WatcherTests()
        {
            this.storage = Substitute.For<IWatcherStorage<Watch>>();
            this.subject = new TestWatcher(this.storage);

            try
            {
                this.subject.ExecuteMatchedWatch = Substitute.For<Func<Watch, ZcoinBlock, int, BlockEventType, bool>>();
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
        public void Constructor_WithNullStorage_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("storage", () => new TestWatcher(null));
        }

        [Fact]
        public async Task StartAsync_WhenSuccess_ShouldStartStorage()
        {
            await this.subject.StartAsync(CancellationToken.None);

            _ = this.storage.Received(1).StartAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StopAsync_WhenSuccess_ShouldStopStorage()
        {
            // Arrange.
            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.storage.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).StopAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task BlockAddedAsync_CreateWatchesAsyncReturnEmptyList_ShouldNotAddToStorage()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            _ = this.storage.Received(0).AddWatchesAsync(Arg.Any<IEnumerable<Watch>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task BlockAddedAsync_CreateWatchesAsyncReturnNonEmptyList_ShouldAddToStorage()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.subject.NewWatches[block] = new[] { watch };

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            _ = this.storage.Received(1).AddWatchesAsync(
                Arg.Is<IEnumerable<Watch>>(l => l.SequenceEqual(new[] { watch })),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockAddedAsync_GetWatchesAsyncReturnEmptyList_ShouldNotCallExecuteMatchedWatchAsync()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            this.subject.ExecuteMatchedWatch.Received(0)(
                Arg.Any<Watch>(),
                Arg.Any<ZcoinBlock>(),
                Arg.Any<int>(),
                Arg.Any<BlockEventType>()
            );

            _ = this.storage.Received(0).RemoveWatchesAsync(
                Arg.Any<IEnumerable<WatchToRemove<Watch>>>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockAddedAsync_ExecuteMatchedWatchAsyncReturnTrue_ShouldRemoveThatWatch()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.subject.Watches[block] = new[] { watch };
            this.subject.ExecuteMatchedWatch(watch, block, 0, BlockEventType.Added).Returns(true);

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            _ = this.storage.Received(1).RemoveWatchesAsync(
                Arg.Is<IEnumerable<WatchToRemove<Watch>>>(l => l.SequenceEqual(new[] { new WatchToRemove<Watch>(watch, WatchRemoveReason.Completed) })),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockAddedAsync_ExecuteMatchedWatchAsyncReturnFalse_ShouldKeepThatWatch()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.subject.Watches[block] = new[] { watch };
            this.subject.ExecuteMatchedWatch(watch, block, 0, BlockEventType.Added).Returns(false);

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            _ = this.storage.Received(0).RemoveWatchesAsync(
                Arg.Any<IEnumerable<WatchToRemove<Watch>>>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockRemovingAsync_GetWatchesAsyncReturnEmptyList_ShouldNotCallExecuteMatchedWatchAsync()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            // Act.
            await this.subject.BlockRemovingAsync(block, 0);

            // Assert.
            this.subject.ExecuteMatchedWatch.Received(0)(
                Arg.Any<Watch>(),
                Arg.Any<ZcoinBlock>(),
                Arg.Any<int>(),
                Arg.Any<BlockEventType>()
            );

            _ = this.storage.Received(0).RemoveWatchesAsync(
                Arg.Any<IEnumerable<WatchToRemove<Watch>>>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockRemovingAsync_ExecuteMatchedWatchAsyncReturnTrue_ShouldRemoveThatWatch()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(uint256.One);

            this.subject.Watches[block] = new[] { watch };
            this.subject.ExecuteMatchedWatch(watch, block, 1, BlockEventType.Removing).Returns(true);

            // Act.
            await this.subject.BlockRemovingAsync(block, 1);

            // Assert.
            _ = this.storage.Received(1).RemoveWatchesAsync(
                Arg.Is<IEnumerable<WatchToRemove<Watch>>>(l => l.SequenceEqual(new[] { new WatchToRemove<Watch>(watch, WatchRemoveReason.Completed) })),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockRemovingAsync_ExecuteMatchedWatchAsyncReturnFalse_ShouldKeepThatWatch()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(uint256.One);

            this.subject.Watches[block] = new[] { watch };
            this.subject.ExecuteMatchedWatch(watch, block, 1, BlockEventType.Removing).Returns(false);

            // Act.
            await this.subject.BlockRemovingAsync(block, 1);

            // Assert.
            _ = this.storage.Received(0).RemoveWatchesAsync(
                Arg.Any<IEnumerable<WatchToRemove<Watch>>>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task BlockRemovingAsync_WatchingOnRemovingBlock_ShouldRemoveThatWatch()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch(block.GetHash());

            this.subject.Watches[block] = new[] { watch };
            this.subject.ExecuteMatchedWatch(watch, block, 0, BlockEventType.Removing).Returns(false);

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
