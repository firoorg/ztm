using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class TransactionConfirmationWatcherTests : IDisposable
    {
        readonly ITransactionConfirmationListener listener1, listener2;
        readonly TestMainDatabaseFactory db;
        readonly IBlockConfirmationListener subject;

        public TransactionConfirmationWatcherTests()
        {
            this.listener1 = Substitute.For<ITransactionConfirmationListener>();
            this.listener1.Id.Returns(Guid.NewGuid());

            this.listener2 = Substitute.For<ITransactionConfirmationListener>();
            this.listener2.Id.Returns(Guid.NewGuid());

            this.db = new TestMainDatabaseFactory();

            try
            {
                this.subject = new TransactionConfirmationWatcher(this.db, new[] { this.listener1, this.listener2 });
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
                () => new TransactionConfirmationWatcher(null, Enumerable.Empty<ITransactionConfirmationListener>())
            );
        }

        [Fact]
        public void Constructor_PassNullForListeners_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "listeners",
                () => new TransactionConfirmationWatcher(this.db, null)
            );
        }

        [Fact]
        public void Id_FromDifferentInstance_ShouldSame()
        {
            IBlockConfirmationListener subject = new TransactionConfirmationWatcher(this.db, Enumerable.Empty<ITransactionConfirmationListener>());

            Assert.Equal(Guid.Parse("a17fd06a-ff3b-4cf7-af66-0c56ea77bc94"), subject.Id);
            Assert.Equal(subject.Id, this.subject.Id);
        }

        [Fact]
        public async Task StartListenAsync_WithEmptyListeners_ShouldReturnFalse()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            IBlockConfirmationListener subject = new TransactionConfirmationWatcher(this.db, Enumerable.Empty<ITransactionConfirmationListener>());

            // Act.
            var enable = await subject.StartListenAsync(block, 0);

            // Assert.
            Assert.False(enable);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task StartListenAsync_NoListenersAcceptWatch_ShouldReturnFalse()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            // Act.
            var enable = await this.subject.StartListenAsync(block, 0);

            // Assert.
            Assert.False(enable);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task StartListenAsync_SomeListenersAcceptWatch_ShouldReturnTrue()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            this.listener2.StartListenAsync((ZcoinTransaction)block.Transactions[0]).Returns(true);

            // Act.
            var enable = await this.subject.StartListenAsync(block, 0);

            // Assert.
            Assert.True(enable);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);
                Assert.Equal(block.Transactions[0].GetHash(), watches[0].Hash);
                Assert.Equal(this.listener2.Id, watches[0].Listener);
            }
        }

        [Fact]
        public async Task BlockConfirmedAsync_NoWatchesRemaining_ShouldReturnFalse()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var transaction = (ZcoinTransaction)block.Transactions[0];

            this.listener1.StartListenAsync(transaction).Returns(true);
            this.listener2.StartListenAsync(transaction).Returns(true);

            await this.subject.StartListenAsync(block, 0);

            // Act.
            var keep = await this.subject.BlockConfirmAsync(block, ConfirmationType.Confirmed, 1);

            // Assert.
            Assert.False(keep);

            _ = this.listener1.Received(1).TransactionConfirmAsync(transaction, ConfirmationType.Confirmed, 1);
            _ = this.listener2.Received(1).TransactionConfirmAsync(transaction, ConfirmationType.Confirmed, 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task BlockConfirmedAsync_HaveWatchesRemaining_ShouldReturnTrue()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var transaction = (ZcoinTransaction)block.Transactions[0];

            this.listener1.StartListenAsync(transaction).Returns(true);
            this.listener1.TransactionConfirmAsync(transaction, ConfirmationType.Confirmed, 1).Returns(true);
            this.listener2.StartListenAsync(transaction).Returns(true);

            await this.subject.StartListenAsync(block, 0);

            // Act.
            var keep = await this.subject.BlockConfirmAsync(block, ConfirmationType.Confirmed, 1);

            // Assert.
            Assert.True(keep);

            _ = this.listener1.Received(1).TransactionConfirmAsync(transaction, ConfirmationType.Confirmed, 1);
            _ = this.listener2.Received(1).TransactionConfirmAsync(transaction, ConfirmationType.Confirmed, 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);
                Assert.Equal(transaction.GetHash(), watches[0].Hash);
                Assert.Equal(this.listener1.Id, watches[0].Listener);
            }
        }

        [Fact]
        public async Task BlockUnconfirmedAsync_NoWatchesRemaining_ShouldReturnFalse()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var transaction = (ZcoinTransaction)block.Transactions[0];

            this.listener1.StartListenAsync(transaction).Returns(true);
            this.listener1.TransactionConfirmAsync(Arg.Any<ZcoinTransaction>(), ConfirmationType.Unconfirming, Arg.Any<int>()).Returns(false);
            this.listener2.StartListenAsync(transaction).Returns(true);
            this.listener2.TransactionConfirmAsync(Arg.Any<ZcoinTransaction>(), ConfirmationType.Unconfirming, Arg.Any<int>()).Returns(false);

            await this.subject.StartListenAsync(block, 0);

            // Act.
            var keep = await this.subject.BlockConfirmAsync(block, ConfirmationType.Unconfirming, 1);

            // Assert.
            Assert.False(keep);

            _ = this.listener1.Received(1).TransactionConfirmAsync(transaction, ConfirmationType.Unconfirming, 1);
            _ = this.listener2.Received(1).TransactionConfirmAsync(transaction, ConfirmationType.Unconfirming, 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task BlockUnconfirmedAsync_HaveWatchesRemaining_ShouldReturnTrue()
        {
            // Arrange.
            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = block0.CreateNextBlockWithCoinbase(
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest),
                1
            );

            var transaction0 = (ZcoinTransaction)block0.Transactions[0];
            var transaction1 = (ZcoinTransaction)block1.Transactions[0];

            this.listener1.StartListenAsync(transaction0).Returns(true);
            this.listener1.StartListenAsync(transaction1).Returns(true);
            this.listener2.StartListenAsync(transaction1).Returns(true);

            this.listener1.TransactionConfirmAsync(transaction0, ConfirmationType.Unconfirming, 2).Returns(true);
            this.listener1.TransactionConfirmAsync(transaction1, ConfirmationType.Unconfirming, 1).Returns(false);
            this.listener2.TransactionConfirmAsync(transaction1, ConfirmationType.Unconfirming, 1).Returns(false);

            await this.subject.StartListenAsync(block0, 0);
            await this.subject.StartListenAsync(block1, 1);

            // Act.
            Assert.False(await this.subject.BlockConfirmAsync(block1, ConfirmationType.Unconfirming, 1));
            Assert.True(await this.subject.BlockConfirmAsync(block0, ConfirmationType.Unconfirming, 2));

            // Assert.
            _ = this.listener1.Received(1).TransactionConfirmAsync(transaction0, ConfirmationType.Unconfirming, 2);
            _ = this.listener1.Received(1).TransactionConfirmAsync(transaction1, ConfirmationType.Unconfirming, 1);
            _ = this.listener2.Received(1).TransactionConfirmAsync(transaction1, ConfirmationType.Unconfirming, 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);
                Assert.Equal(transaction0.GetHash(), watches[0].Hash);
                Assert.Equal(this.listener1.Id, watches[0].Listener);
            }
        }

        [Fact]
        public async Task BlockUnconfirmedAsync_ListenersWantToContinueWatchingButItLastConfirmation_ShouldThrow()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var transaction = (ZcoinTransaction)block.Transactions[0];

            this.listener1.StartListenAsync(transaction).Returns(true);
            this.listener1.TransactionConfirmAsync(Arg.Any<ZcoinTransaction>(), ConfirmationType.Unconfirming, Arg.Any<int>()).Returns(true);
            this.listener2.StartListenAsync(transaction).Returns(true);
            this.listener2.TransactionConfirmAsync(Arg.Any<ZcoinTransaction>(), ConfirmationType.Unconfirming, Arg.Any<int>()).Returns(true);

            await this.subject.StartListenAsync(block, 0);

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.BlockConfirmAsync(block, ConfirmationType.Unconfirming, 1)
            );
        }
    }
}
