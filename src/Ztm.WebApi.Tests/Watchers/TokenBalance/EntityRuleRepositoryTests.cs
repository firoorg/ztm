using System;
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
using TokenBalanceWatcherRule=Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherRule;
using TokenBalanceWatcherRuleStatus=Ztm.Data.Entity.Contexts.Main.TokenBalanceWatcherRuleStatus;
using WebApiCallback=Ztm.Data.Entity.Contexts.Main.WebApiCallback;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class EntityRuleRepositoryTests : IDisposable
    {
        readonly Rule rule;
        readonly Network network;
        readonly TestMainDatabaseFactory db;
        readonly EntityRuleRepository subject;

        public EntityRuleRepositoryTests()
        {
            this.rule = new Rule(
                new PropertyId(3),
                TestAddress.Regtest1,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                "timeout",
                Guid.NewGuid());
            this.network = ZcoinNetworks.Instance.Regtest;
            this.db = new TestMainDatabaseFactory();

            try
            {
                this.subject = new EntityRuleRepository(this.db, this.network);
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
            Assert.Throws<ArgumentNullException>("db", () => new EntityRuleRepository(null, this.network));
        }

        [Fact]
        public void Constructor_WithNullNetwork_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("network", () => new EntityRuleRepository(this.db, null));
        }

        [Fact]
        public Task AddAsync_WithNullRule_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "rule",
                () => this.subject.AddAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_WithNonNullRule_ShouldSaved()
        {
            // Arrange.
            await CreateCallbackAsync(this.rule.Callback);

            // Act.
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(this.rule.Address.ToString(), row.Address);
            Assert.Equal(this.rule.Callback, row.CallbackId);
            Assert.Equal(this.rule.OriginalTimeout, row.OriginalTimeout);
            Assert.Equal(this.rule.OriginalTimeout, row.CurrentTimeout);
            Assert.Equal(this.rule.Property.Value, row.PropertyId);
            Assert.Equal(this.rule.TargetAmount.Indivisible, row.TargetAmount);
            Assert.Equal(this.rule.TargetConfirmation, row.TargetConfirmation);
            Assert.Equal(this.rule.TimeoutStatus, row.TimeoutStatus);
            Assert.Equal(TokenBalanceWatcherRuleStatus.Uncompleted, row.Status);
        }

        [Fact]
        public Task DecreaseTimeoutAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.DecreaseTimeoutAsync(Guid.NewGuid(), TimeSpan.FromHours(1), CancellationToken.None));
        }

        [Fact]
        public async Task DecreaseTimeoutAsync_WithLessValue_StoredRowShouldDecreasedByThatValue()
        {
            // Arrange.
            var value = this.rule.OriginalTimeout / 2;

            await CreateCallbackAsync(this.rule.Callback);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.DecreaseTimeoutAsync(this.rule.Id, value, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(value, row.CurrentTimeout);
            Assert.Equal(this.rule.OriginalTimeout, row.OriginalTimeout);
        }

        [Fact]
        public async Task DecreaseTimeoutAsync_WithGreaterValue_StoredRowShouldBeZero()
        {
            // Arrange.
            var value = this.rule.OriginalTimeout + TimeSpan.FromMilliseconds(1);

            await CreateCallbackAsync(this.rule.Callback);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.DecreaseTimeoutAsync(this.rule.Id, value, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(TimeSpan.Zero, row.CurrentTimeout);
            Assert.Equal(this.rule.OriginalTimeout, row.OriginalTimeout);
        }

        [Fact]
        public Task GetCurrentTimeoutAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.GetCurrentTimeoutAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task GetCurrentTimeoutAsync_WithValidId_ShouldRetrieveCorrectValue()
        {
            // Arrange.
            var expected = this.rule.OriginalTimeout / 2;

            await CreateCallbackAsync(this.rule.Callback);
            await this.subject.AddAsync(this.rule, CancellationToken.None);
            await this.subject.DecreaseTimeoutAsync(this.rule.Id, expected, CancellationToken.None);

            // Act.
            var result = await this.subject.GetCurrentTimeoutAsync(this.rule.Id, CancellationToken.None);

            // Assert.
            Assert.Equal(expected, result);
        }

        [Fact]
        public Task ListUncompletedAsync_WithNullProperty_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentNullException>(
                "property",
                () => this.subject.ListUncompletedAsync(null, CancellationToken.None));
        }

        [Fact]
        public async Task ListUncompletedAsync_WithNonNullProperty_ShouldReturnUncompletedForThatProperty()
        {
            // Arrange.
            var property1 = new PropertyId(3);
            var property2 = new PropertyId(4);

            var rule1 = new Rule(
                property1,
                TestAddress.Regtest1,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                "timeout",
                Guid.NewGuid());

            var rule2 = new Rule(
                property2,
                TestAddress.Regtest2,
                new PropertyAmount(50),
                3,
                TimeSpan.FromMinutes(30),
                "receive-timeout",
                Guid.NewGuid());

            var rule3 = new Rule(
                property2,
                TestAddress.Regtest1,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                "timeout",
                Guid.NewGuid());

            var rule4 = new Rule(
                property2,
                TestAddress.Regtest1,
                new PropertyAmount(100),
                6,
                TimeSpan.FromHours(1),
                "timeout",
                Guid.NewGuid());

            await CreateCallbackAsync(rule1.Callback);
            await this.subject.AddAsync(rule1, CancellationToken.None);

            await CreateCallbackAsync(rule2.Callback);
            await this.subject.AddAsync(rule2, CancellationToken.None);

            await CreateCallbackAsync(rule3.Callback);
            await this.subject.AddAsync(rule3, CancellationToken.None);

            await CreateCallbackAsync(rule4.Callback);
            await this.subject.AddAsync(rule4, CancellationToken.None);

            await this.subject.SetSucceededAsync(rule3.Id, CancellationToken.None);
            await this.subject.SetTimedOutAsync(rule4.Id, CancellationToken.None);

            // Act.
            var result = await this.subject.ListUncompletedAsync(property2, CancellationToken.None);

            // Assert.
            var stored = Assert.Single(result);

            Assert.Equal(rule2.Address, stored.Address);
            Assert.Equal(rule2.Callback, stored.Callback);
            Assert.Equal(rule2.Id, stored.Id);
            Assert.Equal(rule2.OriginalTimeout, stored.OriginalTimeout);
            Assert.Equal(rule2.Property, stored.Property);
            Assert.Equal(rule2.TargetAmount, stored.TargetAmount);
            Assert.Equal(rule2.TargetConfirmation, stored.TargetConfirmation);
            Assert.Equal(rule2.TimeoutStatus, stored.TimeoutStatus);
        }

        [Fact]
        public Task SetSucceededAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.SetSucceededAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task SetSucceededAsync_WithValidId_StoredRowShouldChangedToSucceeded()
        {
            // Arrange.
            await CreateCallbackAsync(this.rule.Callback);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.SetSucceededAsync(this.rule.Id, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(TokenBalanceWatcherRuleStatus.Succeeded, row.Status);
        }

        [Fact]
        public Task SetTimedOutAsync_WithInvalidId_ShouldThrow()
        {
            return Assert.ThrowsAsync<ArgumentException>(
                "id",
                () => this.subject.SetTimedOutAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task SetTimedOutAsync_WithValidId_StoredRowShouldChangedToTimedOut()
        {
            // Arrange.
            await CreateCallbackAsync(this.rule.Callback);
            await this.subject.AddAsync(this.rule, CancellationToken.None);

            // Act.
            await this.subject.SetTimedOutAsync(this.rule.Id, CancellationToken.None);

            // Assert.
            var row = await LoadRuleAsync(this.rule.Id);

            Assert.Equal(TokenBalanceWatcherRuleStatus.TimedOut, row.Status);
        }

        async Task<WebApiCallback> CreateCallbackAsync(Guid id)
        {
            var callback = new WebApiCallback()
            {
                Id = id,
                RegisteredIp = IPAddress.Loopback,
                RegisteredTime = DateTime.Now,
                Completed = false,
                Url = new Uri("http://localhost"),
            };

            using (var db = this.db.CreateDbContext())
            {
                await db.WebApiCallbacks.AddAsync(callback);
                await db.SaveChangesAsync();
            }

            return callback;
        }

        async Task<TokenBalanceWatcherRule> LoadRuleAsync(Guid id)
        {
            using (var db = this.db.CreateDbContext())
            {
                return await db.TokenBalanceWatcherRules.SingleAsync(e => e.Id == id);
            }
        }
    }
}
