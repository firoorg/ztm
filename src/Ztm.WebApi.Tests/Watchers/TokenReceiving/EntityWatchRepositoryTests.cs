using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Testing;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using DomainModel = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenReceiving.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherWatch;
using Status = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherWatchStatus;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class EntityWatchRepositoryTests : IDisposable
    {
        readonly Block block1;
        readonly Block block2;
        readonly Transaction tx1;
        readonly Transaction tx2;
        readonly Transaction tx3;
        readonly Transaction tx4;
        readonly ReceivingAddress address1;
        readonly ReceivingAddress address2;
        readonly ReceivingAddressReservation reservation1;
        readonly ReceivingAddressReservation reservation2;
        readonly Callback callback1;
        readonly Callback callback2;
        readonly Rule rule1;
        readonly Rule rule2;
        readonly DomainModel watch1;
        readonly DomainModel watch2;
        readonly DomainModel watch3;
        readonly DomainModel watch4;
        readonly DomainModel watch5;
        readonly TestMainDatabaseFactory db;
        readonly Mock<IRuleRepository> rules;
        readonly EntityWatchRepository subject;

        public EntityWatchRepositoryTests()
        {
            var network = ZcoinNetworks.Instance.Regtest;

            this.block1 = Block.CreateBlock(network);
            this.block2 = Block.CreateBlock(network);

            this.tx1 = Transaction.Create(network);
            this.tx1.Inputs.Add(TxIn.CreateCoinbase(102));
            this.tx1.Outputs.Add(Money.Coins(30), TestAddress.Regtest1);
            this.tx1.Outputs.Add(Money.Coins(10), TestAddress.Regtest2);

            this.tx2 = Transaction.Create(network);
            this.tx2.Inputs.Add(TxIn.CreateCoinbase(103));
            this.tx2.Outputs.Add(Money.Coins(40), TestAddress.Regtest2);

            this.tx3 = Transaction.Create(network);
            this.tx3.Inputs.Add(this.tx1, 0).ScriptSig = new Script(OpcodeType.OP_0);
            this.tx3.Outputs.Add(Money.Cents(1), TestAddress.Regtest2);

            this.tx4 = Transaction.Create(network);
            this.tx4.Inputs.Add(this.tx1, 1).ScriptSig = new Script(OpcodeType.OP_0);
            this.tx4.Outputs.Add(Money.Cents(1), TestAddress.Regtest2);

            this.block1.AddTransaction(this.tx1);
            this.block2.AddTransaction(this.tx2);
            this.block2.AddTransaction(this.tx3);
            this.block2.AddTransaction(this.tx4);

            this.block1.UpdateMerkleRoot();
            this.block2.UpdateMerkleRoot();

            this.address1 = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest1,
                true,
                new Collection<ReceivingAddressReservation>());

            this.address2 = new ReceivingAddress(
                Guid.NewGuid(),
                TestAddress.Regtest2,
                true,
                new Collection<ReceivingAddressReservation>());

            this.reservation1 = new ReceivingAddressReservation(Guid.NewGuid(), this.address1, DateTime.Now, null);
            this.reservation2 = new ReceivingAddressReservation(Guid.NewGuid(), this.address2, DateTime.Now, null);

            this.callback1 = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.2"),
                DateTime.Now,
                false,
                new Uri("http://localhost/a"));

            this.callback2 = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.3"),
                DateTime.Now,
                false,
                new Uri("http://localhost/b"));

            this.rule1 = new Rule(
                new PropertyId(3),
                this.reservation1,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                "timeout",
                this.callback1);

            this.rule2 = new Rule(
                new PropertyId(4),
                this.reservation2,
                new PropertyAmount(40),
                3,
                TimeSpan.FromMinutes(30),
                "receive-timeout",
                this.callback2);

            this.watch1 = new DomainModel(
                this.rule1,
                this.block1.GetHash(),
                this.tx1.GetHash(),
                this.rule1.AddressReservation.Address.Address,
                new PropertyAmount(100));

            this.watch2 = new DomainModel(
                this.rule2,
                this.block1.GetHash(),
                this.tx1.GetHash(),
                this.rule2.AddressReservation.Address.Address,
                new PropertyAmount(10));

            this.watch3 = new DomainModel(
                this.rule2,
                this.block2.GetHash(),
                this.tx2.GetHash(),
                this.rule2.AddressReservation.Address.Address,
                new PropertyAmount(10));

            this.watch4 = new DomainModel(
                this.rule2,
                this.block2.GetHash(),
                this.tx3.GetHash(),
                this.rule2.AddressReservation.Address.Address,
                new PropertyAmount(10));

            this.watch5 = new DomainModel(
                this.rule2,
                this.block2.GetHash(),
                this.tx4.GetHash(),
                this.rule2.AddressReservation.Address.Address,
                new PropertyAmount(10));

            this.db = new TestMainDatabaseFactory();

            try
            {
                this.rules = new Mock<IRuleRepository>();
                this.rules
                    .Setup(r => r.GetAsync(this.rule1.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.rule1);
                this.rules
                    .Setup(r => r.GetAsync(this.rule2.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.rule2);

                this.subject = new EntityWatchRepository(this.db, this.rules.Object);
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
        public void Constructor_WithNullDb_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("db", () => new EntityWatchRepository(null, this.rules.Object));
        }

        [Fact]
        public void Constructor_WithNullRules_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("rules", () => new EntityWatchRepository(this.db, null));
        }

        [Fact]
        public Task AddAsync_WithNullWatches_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "watches",
                () => this.subject.AddAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_WithNonEmptywatches_ShouldStoreAll()
        {
            // Arrange.
            await StoreRuleAsync(this.rule1);
            await StoreRuleAsync(this.rule2);
            await StoreBlockAsync(this.block1, 102);

            // Act.
            await this.subject.AddAsync(new[] { this.watch1, this.watch2 }, CancellationToken.None);

            // Assert.
            var row1 = await LoadAsync(this.watch1.Id);
            var row2 = await LoadAsync(this.watch2.Id);

            Assert.Equal(this.watch1.Context.Id, row1.RuleId);
            Assert.Equal(this.watch1.BalanceChange.Indivisible, row1.BalanceChange);
            Assert.Equal(this.watch1.Transaction, row1.TransactionId);
            Assert.Equal(this.watch1.StartBlock, row1.BlockId);
            Assert.Equal(this.watch1.StartTime, row1.CreatedTime.ToLocalTime());
            Assert.Equal(0, row1.Confirmation);
            Assert.Equal(Status.Uncompleted, row1.Status);

            Assert.Equal(this.watch2.Context.Id, row2.RuleId);
            Assert.Equal(this.watch2.BalanceChange.Indivisible, row2.BalanceChange);
            Assert.Equal(this.watch2.Transaction, row2.TransactionId);
            Assert.Equal(this.watch2.StartBlock, row2.BlockId);
            Assert.Equal(this.watch2.StartTime, row2.CreatedTime.ToLocalTime());
            Assert.Equal(0, row2.Confirmation);
            Assert.Equal(Status.Uncompleted, row2.Status);
        }

        [Fact]
        public Task ListUncompletedAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => this.subject.ListUncompletedAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task ListUncompletedAsync_WithNonNullProperty_ShouldReturnUncompletedBelongToThatProperty()
        {
            // Arrange.
            await StoreRuleAsync(this.rule1);
            await StoreRuleAsync(this.rule2);
            await StoreBlockAsync(this.block1, 102);
            await StoreBlockAsync(this.block2, 103);

            await this.subject.AddAsync(
                new[] { this.watch1, this.watch2, this.watch3, this.watch4, this.watch5 },
                CancellationToken.None);

            await SetStatusAsync(this.watch3.Id, Status.Succeeded);
            await SetStatusAsync(this.watch4.Id, Status.Rejected);
            await SetStatusAsync(this.watch5.Id, Status.TimedOut);

            // Act.
            var result = await this.subject.ListUncompletedAsync(this.rule2.Property, CancellationToken.None);

            // Assert.
            var uncompleted = Assert.Single(result);

            Assert.Equal(this.watch2.Address, uncompleted.Address);
            Assert.Equal(this.watch2.BalanceChange, uncompleted.BalanceChange);
            Assert.Equal(this.watch2.Transaction, uncompleted.Transaction);
            Assert.Equal(this.watch2.Context, uncompleted.Context);
            Assert.Equal(this.watch2.Id, uncompleted.Id);
            Assert.Equal(this.watch2.StartBlock, uncompleted.StartBlock);
            Assert.Equal(this.watch2.StartTime, uncompleted.StartTime);
        }

        [Fact]
        public Task SetConfirmationCountAsync_WithNullWatches_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "watches",
                () => this.subject.SetConfirmationCountAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task SetConfirmationCountAsync_WithInvalidWatches_ShouldThrow()
        {
            // Arrange.
            await StoreRuleAsync(this.rule1);
            await StoreBlockAsync(this.block1, 102);

            await this.subject.AddAsync(new[] { this.watch1 }, CancellationToken.None);

            // Act.
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                "watches",
                () => this.subject.SetConfirmationCountAsync(
                    new Dictionary<DomainModel, int>()
                    {
                        { this.watch1, 6 },
                        { this.watch2, 6 }
                    },
                    CancellationToken.None));

            // Assert.
            var ids = Assert.IsAssignableFrom<IEnumerable<Guid>>(ex.Data["Identifiers"]);
            var id = Assert.Single(ids);

            Assert.Equal(this.watch2.Id, id);
        }

        [Fact]
        public async Task SetConfirmationCountAsync_WithNonEmptyWatches_StoredConfirmationForThoseWatchesShouldUpdated()
        {
            // Arrange.
            await StoreRuleAsync(this.rule1);
            await StoreRuleAsync(this.rule2);
            await StoreBlockAsync(this.block1, 102);
            await StoreBlockAsync(this.block2, 103);

            await this.subject.AddAsync(new[] { this.watch1, this.watch2, this.watch3 }, CancellationToken.None);

            // Act.
            await this.subject.SetConfirmationCountAsync(
                new Dictionary<DomainModel, int>()
                {
                    { this.watch1, 1 },
                    { this.watch2, 6 }
                },
                CancellationToken.None);

            // Assert.
            var row1 = await LoadAsync(this.watch1.Id);
            var row2 = await LoadAsync(this.watch2.Id);
            var row3 = await LoadAsync(this.watch3.Id);

            Assert.Equal(1, row1.Confirmation);
            Assert.Equal(6, row2.Confirmation);
            Assert.Equal(0, row3.Confirmation);
        }

        [Fact]
        public Task TransitionToRejectedAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => this.subject.TransitionToRejectedAsync(null, uint256.One, CancellationToken.None));
        }

        [Fact]
        public Task TransitionToRejectedAsync_WithNullStartBlock_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "startBlock",
                () => this.subject.TransitionToRejectedAsync(this.rule1.Property, null, CancellationToken.None));
        }

        [Fact]
        public async Task TransitionToRejectedAsync_WithNonNullPropertyAndStartBlock_StatusOfMatchedRowShouldUpdated()
        {
            // Arrange.
            await StoreRuleAsync(this.rule1);
            await StoreRuleAsync(this.rule2);
            await StoreBlockAsync(this.block1, 102);
            await StoreBlockAsync(this.block2, 103);

            await this.subject.AddAsync(
                new[] { this.watch1, this.watch2, this.watch3, this.watch4 },
                CancellationToken.None);

            await this.subject.SetConfirmationCountAsync(
                new Dictionary<DomainModel, int>()
                {
                    { this.watch4, 6 }
                },
                CancellationToken.None);

            await SetStatusAsync(this.watch3.Id, Status.Succeeded);

            // Act.
            var result = await this.subject.TransitionToRejectedAsync(
                this.rule2.Property,
                this.block2.GetHash(),
                CancellationToken.None);

            // Assert.
            var row1 = await LoadAsync(this.watch1.Id);
            var row2 = await LoadAsync(this.watch2.Id);
            var row3 = await LoadAsync(this.watch3.Id);
            var row4 = await LoadAsync(this.watch4.Id);

            Assert.Equal(Status.Uncompleted, row1.Status);
            Assert.Equal(Status.Uncompleted, row2.Status);
            Assert.Equal(Status.Succeeded, row3.Status);
            Assert.Equal(Status.Rejected, row4.Status);

            var completed = Assert.Single(result);

            Assert.Equal(6, completed.Value);
            Assert.Equal(this.watch4, completed.Key);
        }

        [Fact]
        public Task TransitionToSucceededAsync_WithNullWatches_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "watches",
                () => this.subject.TransitionToSucceededAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task TransitionToSucceededAsync_WithNonEmptyWatches_StatusOfMatchedRowShouldUpdated()
        {
            // Arrange.
            await StoreRuleAsync(this.rule1);
            await StoreRuleAsync(this.rule2);
            await StoreBlockAsync(this.block1, 102);
            await StoreBlockAsync(this.block2, 103);

            await this.subject.AddAsync(new[] { this.watch1, this.watch2, this.watch3 }, CancellationToken.None);

            await this.subject.SetConfirmationCountAsync(
                new Dictionary<DomainModel, int>()
                {
                    { this.watch3, 6 }
                },
                CancellationToken.None);

            await SetStatusAsync(this.watch2.Id, Status.Rejected);

            // Act.
            var result = await this.subject.TransitionToSucceededAsync(
                new[] { this.watch2, this.watch3 },
                CancellationToken.None);

            // Assert.
            var row1 = await LoadAsync(this.watch1.Id);
            var row2 = await LoadAsync(this.watch2.Id);
            var row3 = await LoadAsync(this.watch3.Id);

            Assert.Equal(Status.Uncompleted, row1.Status);
            Assert.Equal(Status.Rejected, row2.Status);
            Assert.Equal(Status.Succeeded, row3.Status);

            var completed = Assert.Single(result);

            Assert.Equal(6, completed.Value);
            Assert.Equal(this.watch3, completed.Key);
        }

        [Fact]
        public Task TransitionToTimedOutAsync_WithNullRule_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "rule",
                () => this.subject.TransitionToTimedOutAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task TransitionToTimedOutAsync_WithNonNullRule_StatusOfMatchedRowShouldUpdated()
        {
            // Arrange.
            await StoreRuleAsync(this.rule1);
            await StoreRuleAsync(this.rule2);
            await StoreBlockAsync(this.block1, 102);
            await StoreBlockAsync(this.block2, 103);

            await this.subject.AddAsync(new[] { this.watch1, this.watch2, this.watch3 }, CancellationToken.None);

            await this.subject.SetConfirmationCountAsync(
                new Dictionary<DomainModel, int>()
                {
                    { this.watch3, 6 }
                },
                CancellationToken.None);

            await SetStatusAsync(this.watch2.Id, Status.Rejected);

            // Act.
            var result = await this.subject.TransitionToTimedOutAsync(this.rule2, CancellationToken.None);

            // Assert.
            var row1 = await LoadAsync(this.watch1.Id);
            var row2 = await LoadAsync(this.watch2.Id);
            var row3 = await LoadAsync(this.watch3.Id);

            Assert.Equal(Status.Uncompleted, row1.Status);
            Assert.Equal(Status.Rejected, row2.Status);
            Assert.Equal(Status.TimedOut, row3.Status);

            var completed = Assert.Single(result);

            Assert.Equal(6, completed.Value);
            Assert.Equal(this.watch3, completed.Key);
        }

        async Task StoreBlockAsync(Block block, int height)
        {
            var entity = new Ztm.Data.Entity.Contexts.Main.Block()
            {
                Height = height,
                Hash = block.GetHash(),
                Version = block.Header.Version,
                Bits = block.Header.Bits,
                Nonce = block.Header.Nonce,
                Time = block.Header.BlockTime.UtcDateTime,
                MerkleRoot = block.Header.HashMerkleRoot,
            };

            for (var i = 0; i < block.Transactions.Count; i++)
            {
                var tx = new Ztm.Data.Entity.Contexts.Main.Transaction()
                {
                    Hash = block.Transactions[i].GetHash(),
                    Version = block.Transactions[i].Version,
                    LockTime = block.Transactions[i].LockTime,
                };

                for (var j = 0; j < block.Transactions[i].Inputs.Count; j++)
                {
                    var input = block.Transactions[i].Inputs[j];

                    tx.Inputs.Add(new Ztm.Data.Entity.Contexts.Main.Input()
                    {
                        TransactionHash = tx.Hash,
                        Index = j,
                        OutputHash = input.PrevOut.Hash,
                        OutputIndex = input.PrevOut.N,
                        Script = input.ScriptSig,
                        Sequence = input.Sequence,
                    });
                }

                for (var j = 0; j < block.Transactions[i].Outputs.Count; j++)
                {
                    var output = block.Transactions[i].Outputs[j];

                    tx.Outputs.Add(new Ztm.Data.Entity.Contexts.Main.Output()
                    {
                        TransactionHash = tx.Hash,
                        Index = j,
                        Value = output.Value,
                        Script = output.ScriptPubKey,
                    });
                }

                entity.Transactions.Add(new Ztm.Data.Entity.Contexts.Main.BlockTransaction()
                {
                    BlockHash = entity.Hash,
                    TransactionHash = tx.Hash,
                    Index = i,
                    Transaction = tx,
                });
            }

            using (var db = this.db.CreateDbContext())
            {
                await db.Blocks.AddAsync(entity);
                await db.SaveChangesAsync();
            }
        }

        async Task StoreRuleAsync(Rule rule)
        {
            var callback = new Ztm.Data.Entity.Contexts.Main.WebApiCallback()
            {
                Id = rule.Callback.Id,
                RegisteredIp = rule.Callback.RegisteredIp,
                RegisteredTime = rule.Callback.RegisteredTime,
                Completed = rule.Callback.Completed,
                Url = rule.Callback.Url,
            };

            var address = new Ztm.Data.Entity.Contexts.Main.ReceivingAddress()
            {
                Id = rule.AddressReservation.Address.Id,
                Address = rule.AddressReservation.Address.Address.ToString(),
                IsLocked = rule.AddressReservation.Address.IsLocked,
            };

            var reservation = new Data.Entity.Contexts.Main.ReceivingAddressReservation()
            {
                Id = rule.AddressReservation.Id,
                AddressId = rule.AddressReservation.Address.Id,
                LockedAt = rule.AddressReservation.ReservedDate,
                ReleasedAt = rule.AddressReservation.ReleasedDate,
            };

            var entity = new Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherRule()
            {
                Id = rule.Id,
                CallbackId = rule.Callback.Id,
                PropertyId = rule.Property.Value,
                AddressReservationId = rule.AddressReservation.Id,
                TargetAmount = rule.TargetAmount.Indivisible,
                TargetConfirmation = rule.TargetConfirmation,
                OriginalTimeout = rule.OriginalTimeout,
                CurrentTimeout = rule.OriginalTimeout,
                TimeoutStatus = rule.TimeoutStatus,
                Status = Ztm.Data.Entity.Contexts.Main.TokenReceivingWatcherRuleStatus.Uncompleted,
            };

            using (var db = this.db.CreateDbContext())
            {
                await db.WebApiCallbacks.AddAsync(callback);
                await db.ReceivingAddresses.AddAsync(address);
                await db.ReceivingAddressReservations.AddAsync(reservation);
                await db.TokenReceivingWatcherRules.AddAsync(entity);

                await db.SaveChangesAsync();
            }
        }

        async Task<EntityModel> LoadAsync(Guid id)
        {
            EntityModel entity;

            using (var db = this.db.CreateDbContext())
            {
                entity = await db.TokenReceivingWatcherWatches.SingleAsync(e => e.Id == id);
            }

            entity.CreatedTime = DateTime.SpecifyKind(entity.CreatedTime, DateTimeKind.Utc);

            return entity;
        }

        async Task<EntityModel> SetStatusAsync(Guid id, Status status)
        {
            EntityModel entity;

            using (var db = this.db.CreateDbContext())
            {
                entity = await db.TokenReceivingWatcherWatches.SingleAsync(e => e.Id == id);
                entity.Status = status;

                await db.SaveChangesAsync();
            }

            return entity;
        }
    }
}
