using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public sealed class AddressRulesExecutorTests : IDisposable
    {
        readonly IConfiguration config;
        readonly IAddressRulesStorage storage;
        readonly IRulesExpireWatcher<AddressRule, AddressWatch> expireWatcher;
        readonly IBlocksStorage blocks;
        readonly IRulesExecutor<AddressRule, AddressWatch> subject;

        public AddressRulesExecutorTests()
        {
            this.config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Zcoin:Network:Type", "Regtest"}
            }).Build();

            this.storage = Substitute.For<IAddressRulesStorage>();
            this.expireWatcher = Substitute.For<IRulesExpireWatcher<AddressRule, AddressWatch>>();
            this.blocks = Substitute.For<IBlocksStorage>();
            this.subject = new AddressRulesExecutor(this.config, this.storage, this.expireWatcher, this.blocks);
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "config",
                () => new AddressRulesExecutor(null, this.storage, this.expireWatcher, this.blocks)
            );
        }

        [Fact]
        public void Constructor_WithNullBlocks_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "blocks",
                () => new AddressRulesExecutor(this.config, this.storage, this.expireWatcher, null)
            );
        }

        [Fact]
        public async Task ExecuteRulesAsync_WithMatchedRules_ShouldWatchOnlyMatchedBalanceChangesType()
        {
            // Arrange.
            //
            // Addr1 = Debit
            // Addr2 = Credit
            // Addr3 = Debit | Credit
            // Tx0 = +Addr1
            // Tx1 = -Addr1 +Addr3 +Addr1
            // Tx2 = -Addr1 +Addr2 +Addr1
            // Tx3 = -Addr3
            // 1:TKU6vCuCZr3va4A8sc5ktQqbbdYp5P8512
            // 2:TKL3mmrAc3LtECfA7njpBvzEWtWNxwetZc
            // 3:TQn71kdxYREp9J5jL9LSZpPLzU8uAbSs1Z
            // 4:TMRrhGGKpKTxwePwkLJrwx7d96dXhNeBxY
            var addr1 = BitcoinAddress.Create("TKU6vCuCZr3va4A8sc5ktQqbbdYp5P8512", ZcoinNetworks.Instance.Regtest);
            var addr2 = BitcoinAddress.Create("TKL3mmrAc3LtECfA7njpBvzEWtWNxwetZc", ZcoinNetworks.Instance.Regtest);
            var addr3 = BitcoinAddress.Create("TQn71kdxYREp9J5jL9LSZpPLzU8uAbSs1Z", ZcoinNetworks.Instance.Regtest);
            var addr4 = BitcoinAddress.Create("TMRrhGGKpKTxwePwkLJrwx7d96dXhNeBxY", ZcoinNetworks.Instance.Regtest);

            var rule1 = new AddressRule(addr1, BalanceChangeType.Debit);
            var rule2 = new AddressRule(addr2, BalanceChangeType.Credit);
            var rule3 = new AddressRule(addr3, BalanceChangeType.Debit | BalanceChangeType.Credit);

            var block0 = (ZcoinBlock)ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = (ZcoinBlock)block0.CreateNextBlockWithCoinbase(addr1, 1);
            var block2 = (ZcoinBlock)block1.CreateNextBlockWithCoinbase(addr4, 2);

            var tx1 = new ZcoinTransaction();
            var tx2 = new ZcoinTransaction();
            var tx3 = new ZcoinTransaction();

            // Tx1 = -Addr1 +Addr3 +Addr1
            tx1.Inputs.Add(new ZcoinTxIn()
            {
                PrevOut = new OutPoint(block1.Transactions[0].GetHash(), 0)
            });

            tx1.Outputs.Add(new ZcoinTxOut()
            {
                ScriptPubKey = addr3.ScriptPubKey,
                Value = Money.Coins(10)
            });

            tx1.Outputs.Add(new ZcoinTxOut()
            {
                ScriptPubKey = addr1.ScriptPubKey,
                Value = Money.Coins(20)
            });

            // Tx2 = -Addr1 +Addr2 +Addr1
            tx2.Inputs.Add(new ZcoinTxIn()
            {
                PrevOut = new OutPoint(tx1.GetHash(), 1)
            });

            tx2.Outputs.Add(new ZcoinTxOut()
            {
                ScriptPubKey = addr2.ScriptPubKey,
                Value = Money.Coins(5)
            });

            tx2.Outputs.Add(new ZcoinTxOut()
            {
                ScriptPubKey = addr1.ScriptPubKey,
                Value = Money.Coins(10)
            });

            // Tx3 = -Addr3
            tx3.Inputs.Add(new ZcoinTxIn()
            {
                PrevOut = new OutPoint(tx1.GetHash(), 0)
            });

            block2.Transactions.AddRange(new[] { tx1, tx2, tx3 });

            // Stub dependencies.
            IEnumerable<BitcoinAddress> requested = null;

            this.storage.GetRulesByAddressesAsync(Arg.Do<IEnumerable<BitcoinAddress>>(l => requested = l), Arg.Any<CancellationToken>()).Returns(new[]
            {
                rule1,
                rule2,
                rule3
            });

            this.blocks.GetTransactionAsync(block1.Transactions[0].GetHash(), Arg.Any<CancellationToken>()).Returns((ZcoinTransaction)block1.Transactions[0]);
            this.blocks.GetTransactionAsync(tx1.GetHash(), Arg.Any<CancellationToken>()).Returns(tx1);

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            var watches = await this.subject.ExecuteRulesAsync(block2, 2, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).GetRulesByAddressesAsync(Arg.Any<IEnumerable<BitcoinAddress>>(), Arg.Any<CancellationToken>());

            Assert.Equal(4, requested.Count());
            Assert.Contains(addr1, requested);
            Assert.Contains(addr2, requested);
            Assert.Contains(addr3, requested);
            Assert.Contains(addr4, requested);

            Assert.Equal(4, watches.Count());

            Assert.Equal(AddressWatchType.Debit, watches.ElementAt(0).Type);
            Assert.Same(rule1, watches.ElementAt(0).Rule);
            Assert.Equal(block2.GetHash(), watches.ElementAt(0).StartBlock);

            Assert.Equal(AddressWatchType.Credit, watches.ElementAt(1).Type);
            Assert.Same(rule2, watches.ElementAt(1).Rule);
            Assert.Equal(block2.GetHash(), watches.ElementAt(1).StartBlock);

            Assert.Equal(AddressWatchType.Debit, watches.ElementAt(2).Type);
            Assert.Same(rule3, watches.ElementAt(2).Rule);
            Assert.Equal(block2.GetHash(), watches.ElementAt(2).StartBlock);

            Assert.Equal(AddressWatchType.Credit, watches.ElementAt(3).Type);
            Assert.Same(rule3, watches.ElementAt(3).Rule);
            Assert.Equal(block2.GetHash(), watches.ElementAt(3).StartBlock);
        }

        [Fact]
        public async Task DisassociateRulesAsyc_WhenInvoked_ShouldRemoveOnlyCompletedRules()
        {
            // Arrange.
            var rule0 = new AddressRule(
                BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest),
                BalanceChangeType.Credit
            );

            var rule1 = new AddressRule(
                BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", ZcoinNetworks.Instance.Regtest),
                BalanceChangeType.Debit
            );

            var remove0 = new WatchToRemove<AddressWatch>(
                new AddressWatch(rule0, uint256.One, AddressWatchType.Credit),
                WatchRemoveReason.BlockRemoved
            );

            var remove1 = new WatchToRemove<AddressWatch>(
                new AddressWatch(rule1, uint256.One, AddressWatchType.Debit),
                WatchRemoveReason.Completed | WatchRemoveReason.BlockRemoved
            );

            await this.subject.StartAsync(CancellationToken.None);

            // Act.
            await this.subject.DisassociateRulesAsyc(new[] { remove0, remove1 }, CancellationToken.None);

            // Assert.
            _ = this.storage.Received(1).RemoveRulesAsync(
                Arg.Is<IEnumerable<AddressRule>>(l => l.SequenceEqual(new[] { rule1 })),
                Arg.Any<CancellationToken>()
            );
        }
    }
}
