using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using DomainModel = Ztm.Zcoin.Watching.TransactionWatch<Ztm.WebApi.Watchers.TransactionConfirmation.Rule>;

namespace Ztm.WebApi.Tests.Watchers.TransactionConfirmation
{
    public sealed class EntityWatchRepositoryTests : IDisposable
    {
        readonly TestMainDatabaseFactory databaseFactory;
        readonly EntityCallbackRepository callbackRepository;
        readonly EntityRuleRepository ruleRepository;
        readonly EntityWatchRepository subject;

        public EntityWatchRepositoryTests()
        {
            this.databaseFactory = new TestMainDatabaseFactory();
            this.callbackRepository = new EntityCallbackRepository(this.databaseFactory);
            this.ruleRepository = new EntityRuleRepository(this.databaseFactory);
            this.subject = new EntityWatchRepository(this.databaseFactory);
        }

        public void Dispose()
        {
            this.databaseFactory.Dispose();
        }

        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "db",
                () => new EntityWatchRepository(null));
        }

        [Fact]
        public async Task AddAsync_WithNullWatch_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "watch",
                () => this.subject.AddAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddAsync_WithNullContext_ShouldThrow()
        {
            var watch = new DomainModel(null, uint256.One, uint256.One);

            await Assert.ThrowsAsync<ArgumentException>(
                "watch",
                () => this.subject.AddAsync(watch, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddAsync_WithValidWatch_ShouldSuccess()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new DomainModel(rule, uint256.One, uint256.One);

            // Act.
            await this.subject.AddAsync(watch, CancellationToken.None);

            // Assert.
            using (var db = this.databaseFactory.CreateDbContext())
            {
                var retrieved = await db.TransactionConfirmationWatcherWatches
                    .FirstOrDefaultAsync(w => w.Id == watch.Id, CancellationToken.None);

                Assert.Equal(rule.Id, retrieved.RuleId);
                Assert.Equal(watch.Id, retrieved.Id);
                Assert.Equal(watch.StartBlock, retrieved.StartBlockHash);
                Assert.Equal(watch.StartTime, DateTime.SpecifyKind(retrieved.StartTime, DateTimeKind.Utc));
            }
        }

        [Fact]
        public async Task ListPendingAsync_NoAnyWatches_ShouldReturnEmptyList()
        {
            Assert.Empty(await this.subject.ListPendingAsync(null, CancellationToken.None));
            Assert.Empty(await this.subject.ListPendingAsync(uint256.One, CancellationToken.None));
        }

        [Fact]
        public async Task ListPendingAsync_WithNoPending_ShouldReturnEmptyList()
        {
            // Arrange.
            var rule1 = await GenerateRuleAsync();
            var rule2 = await GenerateRuleAsync();
            var watch1 = new DomainModel(rule1, uint256.One, uint256.One);
            var watch2 = new DomainModel(rule2, uint256.One, uint256.One);

            await this.subject.AddAsync(watch1, CancellationToken.None);
            await this.subject.AddAsync(watch2, CancellationToken.None);

            await this.subject.SetRejectedAsync(watch1.Id, CancellationToken.None);
            await this.subject.SetSucceededAsync(watch2.Id, CancellationToken.None);

            // Act.
            var result1 = await this.subject.ListPendingAsync(null, CancellationToken.None);
            var result2 = await this.subject.ListPendingAsync(uint256.One, CancellationToken.None);

            // Assert.
            Assert.Empty(result1);
            Assert.Empty(result2);
        }

        [Fact]
        public async Task ListPendingAsync_WithPending_ShouldReturnNonEmptyList()
        {
            // Arrange.
            var rule1 = await GenerateRuleAsync();
            var rule2 = await GenerateRuleAsync();
            var rule3 = await GenerateRuleAsync();
            var rule4 = await GenerateRuleAsync();
            var watch1 = new DomainModel(rule1, uint256.One, uint256.One);
            var watch2 = new DomainModel(rule2, uint256.One, uint256.One);
            var watch3 = new DomainModel(rule3, uint256.One, uint256.One);
            var watch4 = new DomainModel(rule4, uint256.Zero, uint256.Zero);

            await this.subject.AddAsync(watch1, CancellationToken.None);
            await this.subject.AddAsync(watch2, CancellationToken.None);
            await this.subject.AddAsync(watch3, CancellationToken.None);
            await this.subject.AddAsync(watch4, CancellationToken.None);

            await this.subject.SetRejectedAsync(watch1.Id, CancellationToken.None);
            await this.subject.SetSucceededAsync(watch2.Id, CancellationToken.None);

            // Act.
            var result1 = await this.subject.ListPendingAsync(null, CancellationToken.None);
            var result2 = await this.subject.ListPendingAsync(uint256.One, CancellationToken.None);

            // Assert.
            result1.Should().BeEquivalentTo(new[] { watch3, watch4 });
            result2.Should().ContainSingle().Which.Should().Be(watch3);
        }

        [Fact]
        public async Task ListRejectedAsync_NoAnyWatches_ShouldReturnEmptyList()
        {
            Assert.Empty(await this.subject.ListRejectedAsync(null, CancellationToken.None));
            Assert.Empty(await this.subject.ListRejectedAsync(uint256.One, CancellationToken.None));
        }

        [Fact]
        public async Task ListRejectedAsync_WithNoRejected_ShouldReturnEmptyList()
        {
            // Arrange.
            var rule1 = await GenerateRuleAsync();
            var rule2 = await GenerateRuleAsync();
            var watch1 = new DomainModel(rule1, uint256.One, uint256.One);
            var watch2 = new DomainModel(rule2, uint256.One, uint256.One);

            await this.subject.AddAsync(watch1, CancellationToken.None);
            await this.subject.AddAsync(watch2, CancellationToken.None);

            await this.subject.SetSucceededAsync(watch2.Id, CancellationToken.None);

            // Act.
            var result1 = await this.subject.ListRejectedAsync(null, CancellationToken.None);
            var result2 = await this.subject.ListRejectedAsync(uint256.One, CancellationToken.None);

            // Assert.
            Assert.Empty(result1);
            Assert.Empty(result2);
        }

        [Fact]
        public async Task ListRejectedAsync_WithRejected_ShouldReturnNonEmptyList()
        {
            // Arrange.
            var rule1 = await GenerateRuleAsync();
            var rule2 = await GenerateRuleAsync();
            var rule3 = await GenerateRuleAsync();
            var rule4 = await GenerateRuleAsync();
            var watch1 = new DomainModel(rule1, uint256.One, uint256.One);
            var watch2 = new DomainModel(rule2, uint256.One, uint256.One);
            var watch3 = new DomainModel(rule3, uint256.One, uint256.One);
            var watch4 = new DomainModel(rule4, uint256.Zero, uint256.Zero);

            await this.subject.AddAsync(watch1, CancellationToken.None);
            await this.subject.AddAsync(watch2, CancellationToken.None);
            await this.subject.AddAsync(watch3, CancellationToken.None);
            await this.subject.AddAsync(watch4, CancellationToken.None);

            await this.subject.SetSucceededAsync(watch2.Id, CancellationToken.None);
            await this.subject.SetRejectedAsync(watch3.Id, CancellationToken.None);
            await this.subject.SetRejectedAsync(watch4.Id, CancellationToken.None);

            // Act.
            var result1 = await this.subject.ListRejectedAsync(null, CancellationToken.None);
            var result2 = await this.subject.ListRejectedAsync(uint256.One, CancellationToken.None);

            // Assert.
            result1.Should().BeEquivalentTo(new[] { watch3, watch4 });
            result2.Should().ContainSingle().Which.Should().Be(watch3);
        }

        [Fact]
        public async Task ListSucceededAsync_NoAnyWatches_ShouldReturnEmptyList()
        {
            Assert.Empty(await this.subject.ListSucceededAsync(null, CancellationToken.None));
            Assert.Empty(await this.subject.ListSucceededAsync(uint256.One, CancellationToken.None));
        }

        [Fact]
        public async Task ListSucceededAsync_WithNoSucceeded_ShouldReturnEmptyList()
        {
            // Arrange.
            var rule1 = await GenerateRuleAsync();
            var rule2 = await GenerateRuleAsync();
            var watch1 = new DomainModel(rule1, uint256.One, uint256.One);
            var watch2 = new DomainModel(rule2, uint256.One, uint256.One);

            await this.subject.AddAsync(watch1, CancellationToken.None);
            await this.subject.AddAsync(watch2, CancellationToken.None);

            await this.subject.SetRejectedAsync(watch2.Id, CancellationToken.None);

            // Act.
            var result1 = await this.subject.ListSucceededAsync(null, CancellationToken.None);
            var result2 = await this.subject.ListSucceededAsync(uint256.One, CancellationToken.None);

            // Assert.
            Assert.Empty(result1);
            Assert.Empty(result2);
        }

        [Fact]
        public async Task ListSucceededAsync_WithSucceeded_ShouldReturnNonEmptyList()
        {
            // Arrange.
            var rule1 = await GenerateRuleAsync();
            var rule2 = await GenerateRuleAsync();
            var rule3 = await GenerateRuleAsync();
            var rule4 = await GenerateRuleAsync();
            var watch1 = new DomainModel(rule1, uint256.One, uint256.One);
            var watch2 = new DomainModel(rule2, uint256.One, uint256.One);
            var watch3 = new DomainModel(rule3, uint256.One, uint256.One);
            var watch4 = new DomainModel(rule4, uint256.Zero, uint256.Zero);

            await this.subject.AddAsync(watch1, CancellationToken.None);
            await this.subject.AddAsync(watch2, CancellationToken.None);
            await this.subject.AddAsync(watch3, CancellationToken.None);
            await this.subject.AddAsync(watch4, CancellationToken.None);

            await this.subject.SetRejectedAsync(watch2.Id, CancellationToken.None);
            await this.subject.SetSucceededAsync(watch3.Id, CancellationToken.None);
            await this.subject.SetSucceededAsync(watch4.Id, CancellationToken.None);

            // Act.
            var result1 = await this.subject.ListSucceededAsync(null, CancellationToken.None);
            var result2 = await this.subject.ListSucceededAsync(uint256.One, CancellationToken.None);

            // Assert.
            result1.Should().BeEquivalentTo(new[] { watch3, watch4 });
            result2.Should().ContainSingle().Which.Should().Be(watch3);
        }

        [Fact]
        public async Task SetRejectedAsync_WithExistWatch_StatusShouldBeUpdated()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new DomainModel(rule, uint256.One, uint256.One);

            await this.subject.AddAsync(watch, CancellationToken.None);

            // Act.
            await this.subject.SetRejectedAsync(watch.Id, CancellationToken.None);

            // Assert.
            var watches = await this.subject.ListRejectedAsync(null, CancellationToken.None);
            Assert.Single(watches);

            var updated = watches.First();
            Assert.Equal(watch.Id, updated.Id);
        }

        [Fact]
        public async Task SetSucceededAsync_WithExistWatch_StatusShouldBeUpdated()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new DomainModel(rule, uint256.One, uint256.One);

            await this.subject.AddAsync(watch, CancellationToken.None);

            // Act.
            await this.subject.SetSucceededAsync(watch.Id, CancellationToken.None);

            // Assert.
            var watches = await this.subject.ListSucceededAsync(null, CancellationToken.None);
            Assert.Single(watches);

            var updated = watches.First();
            Assert.Equal(watch.Id, updated.Id);
        }

        async Task<Rule> GenerateRuleAsync()
        {
            var url = new Uri("https://zcoin.io");
            var success = new CallbackResult("success", "");
            var fail = new CallbackResult("fail", "");
            var callback = await this.callbackRepository.AddAsync(IPAddress.Loopback, url, CancellationToken.None);
            return await this.ruleRepository.AddAsync(uint256.One, 10, TimeSpan.FromHours(1), success, fail, callback, CancellationToken.None);
        }
    }
}
