using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Testing;
using Ztm.WebApi.Watchers.TokenBalance;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using DomainModel = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;
using EntityModel = Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherWatch;
using Status=Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherWatchStatus;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class EntityWatchRepositoryTests : IDisposable
    {
        readonly Network network;
        readonly uint256 block1;
        readonly uint256 block2;
        readonly uint256 tx1;
        readonly uint256 tx2;
        readonly uint256 tx3;
        readonly uint256 tx4;
        readonly Rule rule1;
        readonly Rule rule2;
        readonly DomainModel watch1;
        readonly DomainModel watch2;
        readonly DomainModel watch3;
        readonly DomainModel watch4;
        readonly DomainModel watch5;
        readonly TestMainDatabaseFactory db;
        readonly EntityWatchRepository subject;

        public EntityWatchRepositoryTests()
        {
            this.network = ZcoinNetworks.Instance.Regtest;
            this.block1 = uint256.Parse("70788f475f68e72de2ae9246dabce6c4d5949c8801f6f034aaa151cfce26591d");
            this.block2 = uint256.Parse("504f925d314aa8f991ecfa1ac940b1fa152b377023e0f117a220d10bf957eb8e");
            this.tx1 = uint256.Parse("416517b3b0121379996ed66e4f3a757876acbbe4b90238fb340e5875f9c6692e");
            this.tx2 = uint256.Parse("4e4a2266e350d3409479c7588e85b6ee4f94d004e41d0e3977459564bec40714");
            this.tx3 = uint256.Parse("aab2880bef3fc6965438df55696ae884b91dc36f58e368c2266b719bfaca650f");
            this.tx4 = uint256.Parse("7e07cc21825a6007b1d373f7036320c2478be757b1f1029b8d5f8e406ab69f25");
            this.rule1 = new Rule(
                    new PropertyId(3),
                    TestAddress.Regtest1,
                    new PropertyAmount(100),
                    6,
                    TimeSpan.FromHours(1),
                    "timeout",
                    Guid.NewGuid());
            this.rule2 = new Rule(
                    new PropertyId(4),
                    TestAddress.Regtest2,
                    new PropertyAmount(40),
                    3,
                    TimeSpan.FromMinutes(30),
                    "receive-timeout",
                    Guid.NewGuid());
            this.watch1 = new DomainModel(
                this.rule1,
                this.block1,
                this.tx1,
                this.rule1.Address,
                new PropertyAmount(100));
            this.watch2 = new DomainModel(
                this.rule2,
                this.block1,
                this.tx1,
                this.rule2.Address,
                new PropertyAmount(10));
            this.watch3 = new DomainModel(
                this.rule2,
                this.block2,
                this.tx2,
                this.rule2.Address,
                new PropertyAmount(10));
            this.watch4 = new DomainModel(
                this.rule2,
                this.block2,
                this.tx3,
                this.rule2.Address,
                new PropertyAmount(10));
            this.watch5 = new DomainModel(
                this.rule2,
                this.block2,
                this.tx4,
                this.rule2.Address,
                new PropertyAmount(10));
            this.db = new TestMainDatabaseFactory();

            try
            {
                this.subject = new EntityWatchRepository(this.db, this.network);
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
            Assert.Throws<ArgumentNullException>("db", () => new EntityWatchRepository(null, this.network));
        }

        [Fact]
        public void Constructor_WithNullNetwork_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("network", () => new EntityWatchRepository(this.db, null));
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
                this.block2,
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

            Assert.Equal(6, completed.Confirmation);
            Assert.Equal(this.watch4, completed.Watch);
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

            Assert.Equal(6, completed.Confirmation);
            Assert.Equal(this.watch3, completed.Watch);
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

            Assert.Equal(6, completed.Confirmation);
            Assert.Equal(this.watch3, completed.Watch);
        }

        async Task StoreRuleAsync(Rule rule)
        {
            using (var db = this.db.CreateDbContext())
            {
                await db.WebApiCallbacks.AddAsync(new Data.Entity.Contexts.Main.WebApiCallback()
                {
                    Id = rule.Callback,
                    RegisteredIp = IPAddress.Loopback,
                    RegisteredTime = DateTime.UtcNow,
                    Completed = false,
                    Url = new Uri("http://localhost"),
                });

                await db.TokenBalanceWatcherRules.AddAsync(new Data.Entity.Contexts.Main.TokenBalanceWatcherRule()
                {
                    Id = rule.Id,
                    CallbackId = rule.Callback,
                    PropertyId = rule.Property.Value,
                    Address = rule.Address.ToString(),
                    TargetAmount = rule.TargetAmount.Indivisible,
                    TargetConfirmation = rule.TargetConfirmation,
                    OriginalTimeout = rule.OriginalTimeout,
                    CurrentTimeout = rule.OriginalTimeout,
                    TimeoutStatus = rule.TimeoutStatus,
                    Status = Data.Entity.Contexts.Main.TokenBalanceWatcherRuleStatus.Uncompleted,
                });

                await db.SaveChangesAsync();
            }
        }

        async Task<EntityModel> LoadAsync(Guid id)
        {
            using (var db = this.db.CreateDbContext())
            {
                return await db.TokenBalanceWatcherWatches.SingleAsync(e => e.Id == id);
            }
        }

        async Task<EntityModel> SetStatusAsync(Guid id, Status status)
        {
            EntityModel entity;

            using (var db = this.db.CreateDbContext())
            {
                entity = await db.TokenBalanceWatcherWatches.SingleAsync(e => e.Id == id);
                entity.Status = status;

                await db.SaveChangesAsync();
            }

            return entity;
        }
    }
}
