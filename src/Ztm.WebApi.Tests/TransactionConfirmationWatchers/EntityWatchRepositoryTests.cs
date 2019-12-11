using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.TransactionConfirmationWatchers;
using Ztm.Zcoin.Watching;

namespace Ztm.WebApi.Tests.TransactionConfirmationWatchers
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
        public void AddAsync_WithNullWatch_ShouldThrow()
        {
            _ = Assert.ThrowsAsync<ArgumentNullException>(
                "watch",
                () => this.subject.AddAsync(null, CancellationToken.None));
        }

        [Fact]
        public void AddAsync_WithNullContext_ShouldThrow()
        {
            var watch = new TransactionWatch<Rule>(null, uint256.One, uint256.One);

            _ = Assert.ThrowsAsync<ArgumentNullException>(
                "Context",
                () => this.subject.AddAsync(watch, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_WithValidWatch_ShouldSuccess()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new TransactionWatch<Rule>(rule, uint256.One, uint256.One);

            // Act.
            await this.subject.AddAsync(watch, CancellationToken.None);

            var updatedRule = await this.ruleRepository.GetAsync(rule.Id, CancellationToken.None);
            Assert.Equal(watch.Id, updatedRule.CurrentWatchId);
        }

        [Fact]
        public async Task ListAsync_EmptyWatch_ShouldReturnEmpty()
        {
            var watches = await this.subject.ListAsync(
                WatchStatus.Error,
                CancellationToken.None
            );

            Assert.Empty(watches);
        }

        [Fact]
        public async Task ListAsync_AndNotEmpty_ShouldSuccess()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new TransactionWatch<Rule>(rule, uint256.One, uint256.One);

            await this.subject.AddAsync(watch, CancellationToken.None);

            // Act.
            var watches = await this.subject.ListAsync(WatchStatus.Pending, CancellationToken.None);

            // Assert.
            Assert.Single(watches);
        }

        [Fact]
        public async Task ListAsync_ShouldGetOnlySpecificStatus()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new TransactionWatch<Rule>(rule, uint256.One, uint256.One);
            await this.subject.AddAsync(watch, CancellationToken.None);

            var rule2 = await GenerateRuleAsync();
            var watch2 = new TransactionWatch<Rule>(rule2, uint256.One, uint256.One);
            await this.subject.AddAsync(watch2, CancellationToken.None);

            await this.subject.UpdateStatusAsync(watch2.Id, WatchStatus.Rejected, CancellationToken.None);

            // Act.
            var rejectedWatches = await this.subject.ListAsync(WatchStatus.Rejected, CancellationToken.None);
            var pendingWatches = await this.subject.ListAsync(WatchStatus.Pending, CancellationToken.None);

            // Assert.
            Assert.Single(rejectedWatches);
            Assert.Equal(watch2.Id, rejectedWatches.First().Id);

            Assert.Single(pendingWatches);
            Assert.Equal(watch.Id, pendingWatches.First().Id);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonExistId_ShouldThrow()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => this.subject.UpdateStatusAsync(Guid.NewGuid(), WatchStatus.Error, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateStatusAsync_ExistWatch_ShouldSuccess()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new TransactionWatch<Rule>(rule, uint256.One, uint256.One);

            await this.subject.AddAsync(watch, CancellationToken.None);

            // Act.
            await this.subject.UpdateStatusAsync(watch.Id, WatchStatus.Rejected, CancellationToken.None);

            // Assert.
            var watches = await this.subject.ListAsync(WatchStatus.Rejected, CancellationToken.None);
            Assert.Single(watches);

            var updated = watches.First();
            Assert.Equal(watch.Id, updated.Id);

            var updatedRule = await this.ruleRepository.GetAsync(rule.Id, CancellationToken.None);
            Assert.Null(updatedRule.CurrentWatchId);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithInvalidStatus_ShouldThrow()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new TransactionWatch<Rule>(rule, uint256.One, uint256.One);

            await this.subject.AddAsync(watch, CancellationToken.None);

            // Act & Assert.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.UpdateStatusAsync(watch.Id, WatchStatus.Pending, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateStatusAsync_FinalWatchObject_ShouldThrow()
        {
            // Arrange.
            var rule = await GenerateRuleAsync();
            var watch = new TransactionWatch<Rule>(rule, uint256.One, uint256.One);

            await this.subject.AddAsync(watch, CancellationToken.None);
            await this.subject.UpdateStatusAsync(watch.Id, WatchStatus.Rejected, CancellationToken.None);

            // Act & Assert.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.UpdateStatusAsync(watch.Id, WatchStatus.Success, CancellationToken.None));
        }

        async Task<Rule> GenerateRuleAsync()
        {
            var url = new Uri("https://zcoin.io");
            var success = new FakeCallbackResult("success", "");
            var fail = new FakeCallbackResult("fail", "");
            var callback = await this.callbackRepository.AddAsync(IPAddress.Loopback, url, CancellationToken.None);
            return await this.ruleRepository.AddAsync(uint256.One, 10, TimeSpan.FromHours(1), success, fail, callback, CancellationToken.None);
        }
    }
}