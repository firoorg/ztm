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
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public sealed class ExpirableRulesExecutorTests : IDisposable
    {
        readonly IExpirableRulesStorage<ExpirableRule> storage;
        readonly IRulesExpireWatcher<ExpirableRule, RuledWatch<ExpirableRule>> expireWatcher;
        readonly TestExpirableRulesExecutor subject;

        public ExpirableRulesExecutorTests()
        {
            this.storage = Substitute.For<IExpirableRulesStorage<ExpirableRule>>();
            this.expireWatcher = Substitute.For<IRulesExpireWatcher<ExpirableRule, RuledWatch<ExpirableRule>>>();
            this.subject = new TestExpirableRulesExecutor(this.storage, this.expireWatcher);

            try
            {
                this.subject.DisassociateRule = Substitute.For<Func<RuledWatch<ExpirableRule>, WatchRemoveReason, bool>>();
                this.subject.ExecuteRules = Substitute.For<Func<ZcoinBlock, int, IEnumerable<RuledWatch<ExpirableRule>>>>();
                this.subject.OnRuleExpired = Substitute.For<Action<ExpirableRule>>();
            }
            catch
            {
                this.subject.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public void Constructor_WithNullStorage_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "storage",
                () => new TestExpirableRulesExecutor(null, this.expireWatcher)
            );
        }

        [Fact]
        public void Constructor_WithNullExpireWatcher_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "expireWatcher",
                () => new TestExpirableRulesExecutor(this.storage, null)
            );
        }

        [Fact]
        public async Task StartAsync_WhenSuccess_ShouldResumeExpireWatcherForPreviousSessionRules()
        {
            // Arrange.
            var rule0 = new ExpirableRule();
            var rule1 = new ExpirableRule();

            rule1.ExpirePolicy = new TestExpirePolicy();

            this.storage.GetRulesAsync(Arg.Any<CancellationToken>()).Returns(new[] { rule0, rule1 });

            // Act.
            await this.subject.StartAsync(CancellationToken.None);

            // Assert.
            _ = this.expireWatcher.Received(1).StartAsync(Arg.Any<CancellationToken>());
            _ = this.storage.Received(1).GetRulesAsync(Arg.Any<CancellationToken>());
            _ = this.expireWatcher.Received(0).AddRuleAsync(rule0, Arg.Any<CancellationToken>());
            _ = this.expireWatcher.Received(1).AddRuleAsync(rule1, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StopAsync_WhenSuccess_ShouldStopExpireWatcher()
        {
            // Arrange.
            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.subject.StopAsync(CancellationToken.None);

            // Assert.
            _ = this.expireWatcher.Received(1).StopAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddRuleAsync_WithNullRule_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "rule",
                () => this.subject.AddRuleAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddRuleAsync_WithUnsupportedExpirePolicy_ShouldThrow()
        {
            // Arrange.
            var rule = new ExpirableRule()
            {
                ExpirePolicy = new TestExpirePolicy()
            };

            this.expireWatcher.IsSupported(Arg.Any<Type>()).Returns(false);

            // Act.
            await Assert.ThrowsAsync<ArgumentException>(
                "rule",
                () => this.subject.AddRuleAsync(rule, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddRuleAsync_ExecutorIsNotRuninng_ShouldThrow()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.subject.AddRuleAsync(new ExpirableRule(), CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddRuleAsync_WithNonExpirableRule_ShouldAddToStorageOnly()
        {
            // Arrange.
            var rule = new ExpirableRule();

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.subject.AddRuleAsync(rule, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).AddRuleAsync(rule, Arg.Any<CancellationToken>());
            _ = this.expireWatcher.Received(0).AddRuleAsync(Arg.Any<ExpirableRule>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddRuleAsync_WithExpirableRule_ShouldAddToStorageAndWatchForExpire()
        {
            // Arrange.
            var rule = new ExpirableRule()
            {
                ExpirePolicy = new TestExpirePolicy()
            };

            this.expireWatcher.IsSupported(typeof(TestExpirePolicy)).Returns(true);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.subject.AddRuleAsync(rule, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).AddRuleAsync(rule, Arg.Any<CancellationToken>());
            _ = this.expireWatcher.Received(1).AddRuleAsync(rule, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DisassociateRulesAsyc_WithNullList_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "watches",
                () => this.subject.DisassociateRulesAsyc(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task DisassociateRulesAsyc_ExecutorIsNotRunning_ShouldThrow()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.subject.DisassociateRulesAsyc(
                Enumerable.Empty<WatchToRemove<RuledWatch<ExpirableRule>>>(),
                CancellationToken.None
            ));
        }

        [Fact]
        public async Task DisassociateRulesAsyc_WantToRemoveSomeRules_ShouldRemoveOnlyThoseRules()
        {
            // Arrange.
            var keep = new WatchToRemove<RuledWatch<ExpirableRule>>(
                new RuledWatch<ExpirableRule>(
                    new ExpirableRule()
                    {
                        ExpirePolicy = new TestExpirePolicy()
                    },
                    uint256.One
                ),
                WatchRemoveReason.Completed
            );

            var remove = new WatchToRemove<RuledWatch<ExpirableRule>>(
                new RuledWatch<ExpirableRule>(
                    new ExpirableRule(),
                    uint256.One
                ),
                WatchRemoveReason.Completed
            );

            this.subject.DisassociateRule(keep.Watch, keep.Reason).Returns(false);
            this.subject.DisassociateRule(remove.Watch, remove.Reason).Returns(true);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.subject.DisassociateRulesAsyc(new[] { keep, remove }, CancellationToken.None);

            // Assert.
            _ = this.expireWatcher.Received(1).RemoveReferenceAsync(keep.Watch, true, Arg.Any<CancellationToken>());
            _ = this.expireWatcher.Received(0).RemoveReferenceAsync(remove.Watch, Arg.Any<bool>(), Arg.Any<CancellationToken>());

            _ = this.storage.Received(1).RemoveRulesAsync(
                Arg.Is<IEnumerable<ExpirableRule>>(l => l.SequenceEqual(new[] { remove.Watch.Rule })),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task ExecuteRulesAsync_ExecutorIsNotRunning_ShouldThrow()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ((IRulesExecutor<ExpirableRule, RuledWatch<ExpirableRule>>)this.subject).ExecuteRulesAsync(
                    block,
                    0,
                    CancellationToken.None
                )
            );
        }

        [Fact]
        public async Task ExecuteRulesAsync_HaveMatchedRules_ShouldReturnSuccessfullyReferencedWatches()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            block.Header.PrecomputeHash(invalidateExisting: false, lazily: false);

            var watch0 = new RuledWatch<ExpirableRule>(
                new ExpirableRule(),
                block.GetHash()
            );

            var watch1 = new RuledWatch<ExpirableRule>(
                new ExpirableRule()
                {
                    ExpirePolicy = new TestExpirePolicy()
                },
                block.GetHash()
            );

            var watch2 = new RuledWatch<ExpirableRule>(
                new ExpirableRule()
                {
                    ExpirePolicy = new TestExpirePolicy()
                },
                block.GetHash()
            );

            this.subject.ExecuteRules(block, 0).Returns(new[] { watch0, watch1, watch2 });
            this.expireWatcher.AddReferenceAsync(watch1, Arg.Any<CancellationToken>()).Returns(false);
            this.expireWatcher.AddReferenceAsync(watch2, Arg.Any<CancellationToken>()).Returns(true);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            var result = await ((IRulesExecutor<ExpirableRule, RuledWatch<ExpirableRule>>)this.subject).ExecuteRulesAsync(
                block,
                0,
                CancellationToken.None
            );

            // Assert.
            Assert.Equal(new[] { watch0, watch2 }, result);
        }

        [Fact]
        public async Task ExpireWatcher_WhenRuleExpired_ShouldTriggerExpireHandlerAndRemoveIt()
        {
            // Arrange.
            var rule = new ExpirableRule();

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            var e = new RuleEventArgs<ExpirableRule>(rule, CancellationToken.None);
            this.expireWatcher.RuleExpired += Raise.EventWith(e);
            await Task.WhenAll(e.BackgroundTasks);

            // Assert.
            this.subject.OnRuleExpired.Received(1)(rule);

            _ = this.storage.Received(1).RemoveRulesAsync(
                Arg.Is<IEnumerable<ExpirableRule>>(l => l.SequenceEqual(new[] { rule })),
                Arg.Any<CancellationToken>()
            );
        }
    }
}
