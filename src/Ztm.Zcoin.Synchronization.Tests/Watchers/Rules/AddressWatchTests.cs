using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public class AddressWatchTests
    {
        readonly AddressWatch subject;

        public AddressWatchTests()
        {
            this.subject = new AddressWatch(
                new AddressRule(
                    BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest),
                    BalanceChangeType.Credit | BalanceChangeType.Debit
                ),
                uint256.One,
                AddressWatchType.Credit
            );
        }

        [Fact]
        public void Constructor_WithValidArguments_ShouldInitializeProperties()
        {
            Assert.Equal(AddressWatchType.Credit, this.subject.Type);
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            var other = new AddressWatch(
                this.subject.Rule,
                this.subject.StartBlock,
                AddressWatchType.Debit,
                this.subject.StartTime,
                this.subject.Id
            );

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithSameType_ShouldReturnTrue()
        {
            var other = new AddressWatch(
                this.subject.Rule,
                this.subject.StartBlock,
                this.subject.Type,
                this.subject.StartTime,
                this.subject.Id
            );

            Assert.True(this.subject.Equals(other));
        }
    }
}
