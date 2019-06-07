using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Data.Entity.Testing;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class AddressWatcherTests : IDisposable
    {
        readonly IConfiguration config;
        readonly IBlocksStorage blocks;
        readonly IAddressListener listener1, listener2;
        readonly TestMainDatabaseFactory db;
        readonly ITransactionConfirmationListener subject;

        public AddressWatcherTests()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Zcoin:Network:Type", "Regtest"}
            });

            this.config = builder.Build();
            this.blocks = Substitute.For<IBlocksStorage>();
            this.listener1 = Substitute.For<IAddressListener>();
            this.listener1.Id.Returns(Guid.NewGuid());
            this.listener2 = Substitute.For<IAddressListener>();
            this.listener2.Id.Returns(Guid.NewGuid());
            this.db = new TestMainDatabaseFactory();

            try
            {
                this.subject = new AddressWatcher(this.config, this.db, this.blocks, this.listener1, this.listener2);

                Assert.Equal(Guid.Parse("9b790cf5-53f3-4cce-a1bb-a39ad0ab6c31"), this.subject.Id);
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
        public void Constructor_WithNullConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "config",
                () => new AddressWatcher(null, this.db, this.blocks)
            );
        }

        [Fact]
        public void Constructor_WithNullDb_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "db",
                () => new AddressWatcher(this.config, null, this.blocks)
            );
        }

        [Fact]
        public void Constructor_WithNullBlocks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "blocks",
                () => new AddressWatcher(this.config, this.db, null)
            );
        }

        [Fact]
        public void Constructor_WithNullListeners_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "listeners",
                () => new AddressWatcher(this.config, this.db, this.blocks, null)
            );
        }

        [Fact]
        public async Task StartListenAsync_WithEmptyListeners_ShouldNotWatch()
        {
            // Arrange.
            var subject = new AddressWatcher(this.config, this.db, this.blocks) as ITransactionConfirmationListener;
            var tx = MockTransaction();

            // Act.
            var watched = await subject.StartListenAsync(tx);

            // Assert.
            Assert.False(watched);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task StartListenAsync_ListenerWantToWatchAddressCreditButNoCreditForAddress_ShouldNotWatch()
        {
            // Arrange.
            var tx = MockTransaction();
            var address = BitcoinAddress.Create("TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS", ZcoinNetworks.Instance.Regtest);

            this.listener1.StartListenAsync(address).Returns(AddressListeningType.Credit);

            // Act.
            var watched = await this.subject.StartListenAsync(tx);

            // Assert.
            Assert.False(watched);

            _ = this.listener1.Received(1).StartListenAsync(address);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task StartListenAsync_ListenerWantToWatchAddressDebitButNoDebitForAddress_ShouldNotWatch()
        {
            // Arrange.
            var tx = MockTransaction();
            var address = BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", ZcoinNetworks.Instance.Regtest);

            this.listener2.StartListenAsync(address).Returns(AddressListeningType.Debit);

            // Act.
            var watched = await this.subject.StartListenAsync(tx);

            // Assert.
            Assert.False(watched);

            _ = this.listener2.Received(1).StartListenAsync(address);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task StartListenAsync_ListenerWantToWatchAddressCreditAndDebit_ShouldWatchOnlyMatched()
        {
            // Arrange.
            var tx = MockTransaction();
            var debit = BitcoinAddress.Create("TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS", ZcoinNetworks.Instance.Regtest);
            var credit = BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest);

            this.listener1.StartListenAsync(credit).Returns(AddressListeningType.Credit);
            this.listener2.StartListenAsync(debit).Returns(AddressListeningType.Debit);

            // Act.
            var watched = await this.subject.StartListenAsync(tx);

            // Assert.
            Assert.True(watched);

            _ = this.listener1.Received(1).StartListenAsync(credit);
            _ = this.listener2.Received(1).StartListenAsync(debit);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Equal(2, watches.Length);

                Assert.Equal(debit.ToString(), watches[0].Address);
                Assert.Equal(AddressWatchingType.Debit, watches[0].Type);
                Assert.Equal(this.listener2.Id, watches[0].Listener);

                Assert.Equal(credit.ToString(), watches[1].Address);
                Assert.Equal(AddressWatchingType.Credit, watches[1].Type);
                Assert.Equal(this.listener1.Id, watches[1].Listener);
            }
        }

        [Fact]
        public async Task TransactionConfirmedAsync_SomeWatchesStopped_ShouldReturnTrue()
        {
            // Arrange.
            var tx = MockTransaction();
            var debit = BitcoinAddress.Create("TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS", ZcoinNetworks.Instance.Regtest);
            var credit = BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest);

            this.listener1.StartListenAsync(credit).Returns(AddressListeningType.Credit);
            this.listener2.StartListenAsync(debit).Returns(AddressListeningType.Debit);

            await this.subject.StartListenAsync(tx);

            this.listener1.CreditConfirmedAsync(credit, Money.Coins(3), 1).Returns(true);
            this.listener2.DebitConfirmedAsync(debit, Money.Coins(40), 1).Returns(false);

            // Act.
            var keep = await this.subject.TransactionConfirmedAsync(tx, 1);

            // Assert.
            Assert.True(keep);

            _ = this.listener1.Received(1).CreditConfirmedAsync(credit, Money.Coins(3), 1);
            _ = this.listener2.Received(1).DebitConfirmedAsync(debit, Money.Coins(40), 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);

                Assert.Equal(credit.ToString(), watches[0].Address);
                Assert.Equal(AddressWatchingType.Credit, watches[0].Type);
                Assert.Equal(this.listener1.Id, watches[0].Listener);
            }
        }

        [Fact]
        public async Task TransactionConfirmedAsync_AllWatchesStopped_ShouldReturnFalse()
        {
            // Arrange.
            var tx = MockTransaction();
            var debit = BitcoinAddress.Create("TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS", ZcoinNetworks.Instance.Regtest);
            var credit = BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest);

            this.listener1.StartListenAsync(credit).Returns(AddressListeningType.Credit);
            this.listener2.StartListenAsync(debit).Returns(AddressListeningType.Debit);

            await this.subject.StartListenAsync(tx);

            this.listener1.CreditConfirmedAsync(credit, Money.Coins(3), 1).Returns(false);
            this.listener2.DebitConfirmedAsync(debit, Money.Coins(40), 1).Returns(false);

            // Act.
            var keep = await this.subject.TransactionConfirmedAsync(tx, 1);

            // Assert.
            Assert.False(keep);

            _ = this.listener1.Received(1).CreditConfirmedAsync(credit, Money.Coins(3), 1);
            _ = this.listener2.Received(1).DebitConfirmedAsync(debit, Money.Coins(40), 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        [Fact]
        public async Task TransactionUnconfirmedAsync_SomeWatchesStopped_ShouldReturnTrue()
        {
            // Arrange.
            var tx = MockTransaction();
            var debit = BitcoinAddress.Create("TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS", ZcoinNetworks.Instance.Regtest);
            var credit = BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest);

            this.listener1.StartListenAsync(credit).Returns(AddressListeningType.Credit);
            this.listener2.StartListenAsync(debit).Returns(AddressListeningType.Debit);

            await this.subject.StartListenAsync(tx);

            this.listener1.CreditUnconfirmedAsync(credit, Money.Coins(3), 1).Returns(true);
            this.listener2.DebitUnconfirmedAsync(debit, Money.Coins(40), 1).Returns(false);

            // Act.
            var keep = await this.subject.TransactionUnconfirmedAsync(tx, 1);

            // Assert.
            Assert.True(keep);

            _ = this.listener1.Received(1).CreditUnconfirmedAsync(credit, Money.Coins(3), 1);
            _ = this.listener2.Received(1).DebitUnconfirmedAsync(debit, Money.Coins(40), 1);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Single(watches);

                Assert.Equal(credit.ToString(), watches[0].Address);
                Assert.Equal(AddressWatchingType.Credit, watches[0].Type);
                Assert.Equal(this.listener1.Id, watches[0].Listener);
            }
        }

        [Fact]
        public async Task TransactionUnconfirmedAsync_AllWatchesStopped_ShouldReturnFalse()
        {
            // Arrange.
            var tx = MockTransaction();
            var debit = BitcoinAddress.Create("TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS", ZcoinNetworks.Instance.Regtest);
            var credit = BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest);

            this.listener1.StartListenAsync(credit).Returns(AddressListeningType.Credit);
            this.listener2.StartListenAsync(debit).Returns(AddressListeningType.Debit);

            await this.subject.StartListenAsync(tx);

            this.listener1.CreditUnconfirmedAsync(credit, Money.Coins(3), 0).Returns(true);
            this.listener2.DebitUnconfirmedAsync(debit, Money.Coins(40), 0).Returns(true);

            // Act.
            var keep = await this.subject.TransactionUnconfirmedAsync(tx, 0);

            // Assert.
            Assert.False(keep);

            _ = this.listener1.Received(1).CreditUnconfirmedAsync(credit, Money.Coins(3), 0);
            _ = this.listener2.Received(1).DebitUnconfirmedAsync(debit, Money.Coins(40), 0);

            using (var db = this.db.CreateDbContext())
            {
                var watches = await db.WatchingAddresses.ToArrayAsync(CancellationToken.None);

                Assert.Empty(watches);
            }
        }

        ZcoinTransaction MockTransaction()
        {
            var tx0 = new ZcoinTransaction();
            var tx1 = new ZcoinTransaction();

            tx0.Outputs.Add(new ZcoinTxOut()
            {
                // TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS
                ScriptPubKey = Script.FromHex("21035ac871f3e95106f13ce40f964510738d8706a39020b5110689a245cbc5946e4fac"),
                Value = Money.Coins(40)
            });

            tx0.Outputs.Add(new ZcoinTxOut()
            {
                // TV4gZkmz3VVE8PXUU2gTcn2V1Rg4j2x7HS
                ScriptPubKey = Script.FromHex("21035ac871f3e95106f13ce40f964510738d8706a39020b5110689a245cbc5946e4fac"),
                Value = Money.Coins(30)
            });

            tx1.Inputs.Add(new ZcoinTxIn()
            {
                PrevOut = new OutPoint(tx0.GetHash(), 0)
            });

            tx1.Outputs.Add(new ZcoinTxOut()
            {
                // TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz
                ScriptPubKey = Script.FromHex("76a914429b6d269725691af974ecf592f00697b6174bb488ac"),
                Value = Money.Coins(30)
            });

            tx1.Outputs.Add(new ZcoinTxOut()
            {
                // TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE
                ScriptPubKey = Script.FromHex("76a914cf74e727be79361f3016a9069e276fd678140a1988ac"),
                Value = Money.Coins(1)
            });

            tx1.Outputs.Add(new ZcoinTxOut()
            {
                // TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE
                ScriptPubKey = Script.FromHex("76a914cf74e727be79361f3016a9069e276fd678140a1988ac"),
                Value = Money.Coins(2)
            });

            this.blocks.GetTransactionAsync(tx0.GetHash(), Arg.Any<CancellationToken>()).Returns(tx0);

            return tx1;
        }
    }
}
