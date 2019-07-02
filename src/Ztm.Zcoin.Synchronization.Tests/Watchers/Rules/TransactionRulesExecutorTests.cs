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
    public sealed class TransactionRulesExecutorTests : IDisposable
    {
        readonly ITransactionRulesStorage storage;
        readonly IRulesExpireWatcher<TransactionRule, TransactionWatch> expireWatcher;
        readonly IRulesExecutor<TransactionRule, TransactionWatch> subject;

        public TransactionRulesExecutorTests()
        {
            this.storage = Substitute.For<ITransactionRulesStorage>();
            this.expireWatcher = Substitute.For<IRulesExpireWatcher<TransactionRule, TransactionWatch>>();
            this.subject = new TransactionRulesExecutor(this.storage, this.expireWatcher);
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public async Task ExecuteRulesAsync_WhenInvoke_ShouldCreateWatchesForMatchedRules()
        {
            // Arrange.
            var block = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var transactions = block.Transactions.Select(t => t.GetHash()).ToArray();
            var rule1 = new TransactionRule(uint256.One);
            var rule2 = new TransactionRule(block.Transactions[0].GetHash());

            this.storage.GetRulesByTransactionHashesAsync(
                Arg.Is<IEnumerable<uint256>>(p => p.SequenceEqual(transactions)),
                Arg.Any<CancellationToken>()
            ).Returns(new[] { rule2 });

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            var watches = await this.subject.ExecuteRulesAsync(block, 0, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).GetRulesByTransactionHashesAsync(
                Arg.Is<IEnumerable<uint256>>(p => p.SequenceEqual(transactions)),
                Arg.Any<CancellationToken>()
            );

            Assert.Single(watches);
            Assert.Same(rule2, watches.Single().Rule);
            Assert.Equal(block.GetHash(), watches.Single().StartBlock);
        }

        [Fact]
        public async Task DisassociateRulesAsyc_WhenInvoke_ShouldRemoveOnlyCompletedRules()
        {
            // Arrange.
            var rule1 = new TransactionRule(uint256.Zero);
            var rule2 = new TransactionRule(uint256.One);

            var remove1 = new WatchToRemove<TransactionWatch>(
                new TransactionWatch(rule1, uint256.One),
                WatchRemoveReason.BlockRemoved
            );

            var remove2 = new WatchToRemove<TransactionWatch>(
                new TransactionWatch(rule2, uint256.One),
                WatchRemoveReason.Completed | WatchRemoveReason.BlockRemoved
            );

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.subject.DisassociateRulesAsyc(new[] { remove1, remove2 }, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).RemoveRulesAsync(
                Arg.Is<IEnumerable<TransactionRule>>(p => p.SequenceEqual(new[] { rule2 })),
                Arg.Any<CancellationToken>()
            );
        }
    }
}
