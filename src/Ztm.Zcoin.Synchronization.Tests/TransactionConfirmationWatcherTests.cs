using System;
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
                this.subject = new TransactionConfirmationWatcher(this.db, this.listener1, this.listener2);
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
                () => new TransactionConfirmationWatcher(null)
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
            IBlockConfirmationListener subject = new TransactionConfirmationWatcher(this.db);

            Assert.Equal(Guid.Parse("a17fd06a-ff3b-4cf7-af66-0c56ea77bc94"), subject.Id);
            Assert.Equal(subject.Id, this.subject.Id);
        }

        [Fact]
        public async Task StartListenAsync_WithEmptyListeners_ShouldReturnFalse()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            IBlockConfirmationListener subject = new TransactionConfirmationWatcher(this.db);

            // Act.
            var enable = await subject.StartListenAsync(block, 0, CancellationToken.None);

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
            var enable = await this.subject.StartListenAsync(block, 0, CancellationToken.None);

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

            this.listener2.StartListenAsync((ZcoinTransaction)block.Transactions[0], Arg.Any<CancellationToken>()).Returns(true);

            // Act.
            var enable = await this.subject.StartListenAsync(block, 0, CancellationToken.None);

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

            this.listener1.StartListenAsync(transaction, Arg.Any<CancellationToken>()).Returns(true);
            this.listener2.StartListenAsync(transaction, Arg.Any<CancellationToken>()).Returns(true);

            await this.subject.StartListenAsync(block, 0, CancellationToken.None);

            // Act.
            var keep = await this.subject.BlockConfirmedAsync(block, 1);

            // Assert.
            Assert.False(keep);

            _ = this.listener1.Received(1).TransactionConfirmedAsync(transaction, 1);
            _ = this.listener2.Received(1).TransactionConfirmedAsync(transaction, 1);

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

            this.listener1.StartListenAsync(transaction, Arg.Any<CancellationToken>()).Returns(true);
            this.listener1.TransactionConfirmedAsync(transaction, 1).Returns(true);
            this.listener2.StartListenAsync(transaction, Arg.Any<CancellationToken>()).Returns(true);

            await this.subject.StartListenAsync(block, 0, CancellationToken.None);

            // Act.
            var keep = await this.subject.BlockConfirmedAsync(block, 1);

            // Assert.
            Assert.True(keep);

            _ = this.listener1.Received(1).TransactionConfirmedAsync(transaction, 1);
            _ = this.listener2.Received(1).TransactionConfirmedAsync(transaction, 1);

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

            this.listener1.StartListenAsync(transaction, Arg.Any<CancellationToken>()).Returns(true);
            this.listener1.TransactionUnconfirmedAsync(Arg.Any<ZcoinTransaction>(), Arg.Any<int>()).Returns(true);
            this.listener2.StartListenAsync(transaction, Arg.Any<CancellationToken>()).Returns(true);
            this.listener2.TransactionUnconfirmedAsync(Arg.Any<ZcoinTransaction>(), Arg.Any<int>()).Returns(true);

            await this.subject.StartListenAsync(block, 0, CancellationToken.None);

            // Act.
            var keep = await this.subject.BlockUnconfirmedAsync(block, 0);

            // Assert.
            Assert.False(keep);

            _ = this.listener1.Received(1).TransactionUnconfirmedAsync(transaction, 0);
            _ = this.listener2.Received(1).TransactionUnconfirmedAsync(transaction, 0);

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

            this.listener1.StartListenAsync(transaction0, Arg.Any<CancellationToken>()).Returns(true);
            this.listener1.StartListenAsync(transaction1, Arg.Any<CancellationToken>()).Returns(true);
            this.listener2.StartListenAsync(transaction1, Arg.Any<CancellationToken>()).Returns(true);

            this.listener1.TransactionUnconfirmedAsync(Arg.Any<ZcoinTransaction>(), Arg.Any<int>()).Returns(true);
            this.listener2.TransactionUnconfirmedAsync(Arg.Any<ZcoinTransaction>(), Arg.Any<int>()).Returns(true);

            await this.subject.StartListenAsync(block0, 0, CancellationToken.None);
            await this.subject.StartListenAsync(block1, 1, CancellationToken.None);

            // Act.
            Assert.False(await this.subject.BlockUnconfirmedAsync(block1, 0));
            Assert.True(await this.subject.BlockUnconfirmedAsync(block0, 1));

            // Assert.
            _ = this.listener1.Received(1).TransactionUnconfirmedAsync(transaction0, 1);
            _ = this.listener1.Received(1).TransactionUnconfirmedAsync(transaction1, 0);
            _ = this.listener2.Received(1).TransactionUnconfirmedAsync(transaction1, 0);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingTransactions.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);
                Assert.Equal(transaction0.GetHash(), watches[0].Hash);
                Assert.Equal(this.listener1.Id, watches[0].Listener);
            }
        }
    }
}
