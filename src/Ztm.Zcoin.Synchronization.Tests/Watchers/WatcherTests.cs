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
    public sealed class WatcherTests
    {
        readonly IWatcherHandler<Watch<object>, object> handler;
        readonly TestWatcher subject;

        public WatcherTests()
        {
            this.handler = Substitute.For<IWatcherHandler<Watch<object>, object>>();
            this.subject = new TestWatcher(this.handler);
            this.subject.CreateWatches = Substitute.For<Func<Block, int, CancellationToken, IEnumerable<Watch<object>>>>();
            this.subject.ExecuteMatchedWatch = Substitute.For<Func<Watch<object>, Block, int, BlockEventType, CancellationToken, bool>>();
            this.subject.GetWatches = Substitute.For<Func<Block, int, CancellationToken, IEnumerable<Watch<object>>>>();
        }

        [Fact]
        public void Constructor_WithNullStorage_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("handler", () => new TestWatcher(null));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullBlock_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => this.subject.ExecuteAsync(null, 0, BlockEventType.Added, CancellationToken.None)
            );
        }

        [Fact]
        public async Task ExecuteAsync_WhenInvokeWithBlockAdded_ShouldInvokeCreateWatchesAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.subject.GetWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                this.subject.CreateWatches.Received(1)(block, 0, cancellationSource.Token);
            }
        }

        [Fact]
        public async Task ExecuteAsync_WhenInvoke_ShouldAlwaysInvokeGetWatchesAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.subject.GetWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Removing, cancellationSource.Token);

                // Assert.
                this.subject.GetWatches.Received(2)(block, 0, cancellationSource.Token);
            }
        }

        [Fact]
        public async Task ExecuteAsync_CreateWatchesAsyncReturnEmptyList_ShouldNotInvokeAddWatchesAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.subject.CreateWatches(block, 0, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.subject.GetWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());

            // Act.
            await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, CancellationToken.None);

            // Assert.
            _ = this.handler.Received(0).AddWatchesAsync(Arg.Any<IEnumerable<Watch<object>>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_CreateWatchesAsyncReturnNonEmptyList_ShouldInvokeAddWatchesAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch<object>(null, block.GetHash());

            this.subject.CreateWatches(block, 0, Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.subject.GetWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                _ = this.handler.Received(1).AddWatchesAsync(
                    Arg.Is<IEnumerable<Watch<object>>>(l => l.SequenceEqual(new[] { watch })),
                    cancellationSource.Token
                );
            }
        }

        [Fact]
        public async Task ExecuteAsync_GetWatchesAsyncReturnEmptyList_ShouldNotCallExecuteMatchedWatchAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.subject.GetWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                this.subject.ExecuteMatchedWatch.Received(0)(
                    Arg.Any<Watch<object>>(),
                    Arg.Any<Block>(),
                    Arg.Any<int>(),
                    Arg.Any<BlockEventType>(),
                    Arg.Any<CancellationToken>()
                );
            }
        }

        [Fact]
        public async Task ExecuteAsync_ExecuteMatchedWatchAsyncReturnTrue_ShouldInvokeRemoveWatchAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch<object>(null, block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.subject.GetWatches(block, 0, Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.subject.ExecuteMatchedWatch(watch, block, 0, BlockEventType.Added, Arg.Any<CancellationToken>()).Returns(true);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                this.subject.ExecuteMatchedWatch.Received(1)(watch, block, 0, BlockEventType.Added, CancellationToken.None);
                _ = this.handler.Received(1).RemoveWatchAsync(watch, WatchRemoveReason.Completed, CancellationToken.None);
            }
        }

        [Fact]
        public async Task ExecuteAsync_ExecuteMatchedWatchAsyncReturnFalse_ShouldNotInvokeRemoveWatchAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch<object>(null, block.GetHash());

            this.subject.CreateWatches(Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Watch<object>>());
            this.subject.GetWatches(block, 0, Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.subject.ExecuteMatchedWatch(watch, block, 0, BlockEventType.Added, Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Added, cancellationSource.Token);

                // Assert.
                this.subject.ExecuteMatchedWatch.Received(1)(watch, block, 0, BlockEventType.Added, CancellationToken.None);
                _ = this.handler.Received(0).RemoveWatchAsync(Arg.Any<Watch<object>>(), Arg.Any<WatchRemoveReason>(), Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public async Task BlockRemovingAsync_WatchingOnRemovingBlock_ShouldInvokeRemoveWatchAsync()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var watch = new Watch<object>(null, block.GetHash());

            this.subject.GetWatches(block, 0, Arg.Any<CancellationToken>()).Returns(new[] { watch });
            this.subject.ExecuteMatchedWatch(Arg.Any<Watch<object>>(), Arg.Any<Block>(), Arg.Any<int>(), Arg.Any<BlockEventType>(), Arg.Any<CancellationToken>()).Returns(false);

            // Act.
            using (var cancellationSource = new CancellationTokenSource())
            {
                await this.subject.ExecuteAsync(block, 0, BlockEventType.Removing, cancellationSource.Token);

                // Assert.
                this.subject.ExecuteMatchedWatch.Received(1)(watch, block, 0, BlockEventType.Removing, CancellationToken.None);
                _ = this.handler.Received(1).RemoveWatchAsync(watch, WatchRemoveReason.BlockRemoved, CancellationToken.None);
            }
        }
    }
}