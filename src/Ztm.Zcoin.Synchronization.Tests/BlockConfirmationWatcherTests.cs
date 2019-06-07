using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class BlockConfirmationWatcherTests : IDisposable
    {
        readonly TestMainDatabaseFactory db;
        readonly IBlocksStorage storage;
        readonly IBlockConfirmationListener listener1, listener2;
        readonly IBlockListener subject;

        public BlockConfirmationWatcherTests()
        {
            this.listener1 = Substitute.For<IBlockConfirmationListener>();
            this.listener1.Id.Returns(Guid.NewGuid());
            this.listener2 = Substitute.For<IBlockConfirmationListener>();
            this.listener2.Id.Returns(Guid.NewGuid());

            this.storage = Substitute.For<IBlocksStorage>();
            this.db = new TestMainDatabaseFactory();

            try
            {
                this.subject = new BlockConfirmationWatcher(this.db, this.storage, this.listener1, this.listener2);
            }
            catch
            {
                this.db.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.db.Dispose();
        }

        [Fact]
        public void Constructor_PassNullForDb_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "db",
                () => new BlockConfirmationWatcher(null, this.storage)
            );
        }

        [Fact]
        public void Constructor_PassNullForBlocks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "blocks",
                () => new BlockConfirmationWatcher(this.db, null)
            );
        }

        [Fact]
        public void Constructor_PassNullForListeners_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "listeners",
                () => new BlockConfirmationWatcher(this.db, this.storage, null)
            );
        }

        [Fact]
        public async Task BlockAddedAsync_WithEmptyListeners_ShouldSuccess()
        {
            // Arrange.
            IBlockListener subject = new BlockConfirmationWatcher(this.db, this.storage);
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            // Act.
            await subject.BlockAddedAsync(block, 0);
        }

        [Fact]
        public async Task BlockAddedAsync_StartListenAsyncReturnTrue_ShouldWatchThatBlock()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.listener1.StartListenAsync(block, 0).Returns(false);
            this.listener1.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), Arg.Any<ConfirmationType>(), Arg.Any<int>()).Returns(Task.FromResult(false));

            this.listener2.StartListenAsync(block, 0).Returns(true);
            this.listener2.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), Arg.Any<ConfirmationType>(), Arg.Any<int>()).Returns(Task.FromResult(true));

            // Act.
            await this.subject.BlockAddedAsync(block, 0);

            // Assert.
            _ = this.listener1.Received(0).BlockConfirmAsync(block, ConfirmationType.Confirmed, 1);
            _ = this.listener2.Received(1).BlockConfirmAsync(block, ConfirmationType.Confirmed, 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingBlocks.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);
                Assert.Equal(block.GetHash(), watches[0].Hash);
                Assert.Equal(this.listener2.Id, watches[0].Listener);
            }
        }

        [Fact]
        public async Task BlockAddedAsync_WithPreviousWatches_ShouldInvokeListenersBothPreviousAndNewWatches()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            this.storage.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));
            this.storage.GetAsync(block1.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block1, height: 1));

            this.listener1.StartListenAsync(block0, 0).Returns(true);
            this.listener1.StartListenAsync(block1, 1).Returns(false);
            this.listener1.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), Arg.Any<ConfirmationType>(), Arg.Any<int>()).Returns(Task.FromResult(true));

            this.listener2.StartListenAsync(block0, 0).Returns(false);
            this.listener2.StartListenAsync(block1, 1).Returns(true);
            this.listener2.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), Arg.Any<ConfirmationType>(), Arg.Any<int>()).Returns(Task.FromResult(true));

            await this.subject.BlockAddedAsync(block0, 0);

            // Act.
            await this.subject.BlockAddedAsync(block1, 1);

            // Assert.
            _ = this.listener1.Received(1).BlockConfirmAsync(block0, ConfirmationType.Confirmed, 1);
            _ = this.listener1.Received(1).BlockConfirmAsync(block0, ConfirmationType.Confirmed, 2);
            _ = this.listener1.Received(0).BlockConfirmAsync(block1, ConfirmationType.Confirmed, Arg.Any<int>());
            _ = this.listener2.Received(0).BlockConfirmAsync(block0, ConfirmationType.Confirmed, Arg.Any<int>());
            _ = this.listener2.Received(1).BlockConfirmAsync(block1, ConfirmationType.Confirmed, 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingBlocks.ToArrayAsync(CancellationToken.None);

                Assert.Equal(2, watches.Length);
                Assert.Equal(block0.GetHash(), watches[0].Hash);
                Assert.Equal(this.listener1.Id, watches[0].Listener);
                Assert.Equal(block1.GetHash(), watches[1].Hash);
                Assert.Equal(this.listener2.Id, watches[1].Listener);
            }
        }

        [Fact]
        public async Task BlockAddedAsync_BlockConfirmedAsyncReturnFalse_ShouldRemoveThatWatch()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            this.storage.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));
            this.storage.GetAsync(block1.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block1, height: 1));

            this.listener1.StartListenAsync(block0, 0).Returns(true);
            this.listener1.StartListenAsync(block1, 1).Returns(false);
            this.listener1.BlockConfirmAsync(block0, ConfirmationType.Confirmed, 1).Returns(Task.FromResult(true));
            this.listener1.BlockConfirmAsync(block0, ConfirmationType.Confirmed, 2).Returns(Task.FromResult(false));

            this.listener2.StartListenAsync(block0, 0).Returns(false);
            this.listener2.StartListenAsync(block1, 1).Returns(true);
            this.listener2.BlockConfirmAsync(block1, ConfirmationType.Confirmed, 1).Returns(Task.FromResult(false));

            await this.subject.BlockAddedAsync(block0, 0);

            // Act.
            await this.subject.BlockAddedAsync(block1, 1);

            // Assert.
            _ = this.listener1.Received(1).BlockConfirmAsync(block0, ConfirmationType.Confirmed, 1);
            _ = this.listener1.Received(1).BlockConfirmAsync(block0, ConfirmationType.Confirmed, 2);
            _ = this.listener1.Received(0).BlockConfirmAsync(block1, ConfirmationType.Confirmed, Arg.Any<int>());
            _ = this.listener2.Received(0).BlockConfirmAsync(block0, ConfirmationType.Confirmed, Arg.Any<int>());
            _ = this.listener2.Received(1).BlockConfirmAsync(block1, ConfirmationType.Confirmed, 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingBlocks.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task BlockRemovedAsync_WithEmptyWatches_ShouldSuccess()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            await this.subject.BlockAddedAsync(block0, 0);

            // Act.
            await this.subject.BlockRemovingAsync(block0, 0);
        }

        [Fact]
        public async Task BlockRemovedAsync_BlockUnconfirmedAsyncReturnFalse_ShouldRemoveThatWatch()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            this.storage.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));
            this.storage.GetAsync(block1.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block1, height: 1));

            this.listener1.StartListenAsync(block0, 0).Returns(true);
            this.listener1.StartListenAsync(block1, 1).Returns(true);
            this.listener1.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), ConfirmationType.Confirmed, Arg.Any<int>()).Returns(Task.FromResult(true));
            this.listener1.BlockConfirmAsync(block0, ConfirmationType.Unconfirming, 2).Returns(true);
            this.listener1.BlockConfirmAsync(block1, ConfirmationType.Unconfirming, 1).Returns(false);

            this.listener2.StartListenAsync(block0, 0).Returns(true);
            this.listener2.StartListenAsync(block1, 1).Returns(true);
            this.listener2.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), ConfirmationType.Confirmed, Arg.Any<int>()).Returns(Task.FromResult(true));
            this.listener2.BlockConfirmAsync(block0, ConfirmationType.Unconfirming, 2).Returns(false);
            this.listener2.BlockConfirmAsync(block1, ConfirmationType.Unconfirming, 1).Returns(false);

            await this.subject.BlockAddedAsync(block0, 0);
            await this.subject.BlockAddedAsync(block1, 1);

            // Act.
            await this.subject.BlockRemovingAsync(block1, 1);

            // Assert.
            _ = this.listener1.Received(1).BlockConfirmAsync(block1, ConfirmationType.Unconfirming, 1);
            _ = this.listener1.Received(1).BlockConfirmAsync(block0, ConfirmationType.Unconfirming, 2);
            _ = this.listener2.Received(1).BlockConfirmAsync(block1, ConfirmationType.Unconfirming, 1);
            _ = this.listener2.Received(1).BlockConfirmAsync(block0, ConfirmationType.Unconfirming, 2);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingBlocks.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);
                Assert.Equal(block0.GetHash(), watches[0].Hash);
                Assert.Equal(this.listener1.Id, watches[0].Listener);
            }
        }

        [Fact]
        public async Task BlockRemovedAsync_ListenersWantToContinueWatchingButItLastConfirmation_ShouldThrow()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = ZcoinBlock.CreateBlock(ZcoinNetworks.Instance.Regtest);

            this.storage.GetAsync(block0.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block0, height: 0));
            this.storage.GetAsync(block1.GetHash(), Arg.Any<CancellationToken>()).Returns((block: block1, height: 1));

            this.listener1.StartListenAsync(block0, 0).Returns(true);
            this.listener1.StartListenAsync(block1, 1).Returns(true);
            this.listener1.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), Arg.Any<ConfirmationType>(), Arg.Any<int>()).Returns(Task.FromResult(true));

            this.listener2.StartListenAsync(block0, 0).Returns(true);
            this.listener2.StartListenAsync(block1, 1).Returns(true);
            this.listener2.BlockConfirmAsync(Arg.Any<ZcoinBlock>(), Arg.Any<ConfirmationType>(), Arg.Any<int>()).Returns(Task.FromResult(true));

            await this.subject.BlockAddedAsync(block0, 0);
            await this.subject.BlockAddedAsync(block1, 1);

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.BlockRemovingAsync(block1, 1)
            );
        }
    }
}
