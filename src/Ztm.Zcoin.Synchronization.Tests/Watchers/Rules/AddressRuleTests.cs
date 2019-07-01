using System;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public class AddressRuleTests
    {
        readonly AddressRule subject;

        public AddressRuleTests()
        {
            this.subject = new AddressRule(
                BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest),
                BalanceChangeType.Credit | BalanceChangeType.Debit
            );
        }

        [Fact]
        public void Constructor_WithNullAddress_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new AddressRule(null, BalanceChangeType.Credit)
            );
        }

        [Fact]
        public void Constructor_WithValidArguments_ShouldInitializeProperties()
        {
            Assert.Equal(BitcoinAddress.Create("TUt8vKwCj6UnWDB35eszaRGEAhtzoLZmaE", ZcoinNetworks.Instance.Regtest), this.subject.Address);
            Assert.Equal(BalanceChangeType.Credit | BalanceChangeType.Debit, this.subject.BalanceChangeType);
        }

        [Fact]
        public void Equals_WithDifferentAddress_ShouldReturnFalse()
        {
            var other = new AddressRule(
                BitcoinAddress.Create("TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz", ZcoinNetworks.Instance.Regtest),
                this.subject.BalanceChangeType,
                this.subject.Id
            );

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithDifferentBalanceChangeType_ShouldReturnFalse()
        {
            var other = new AddressRule(this.subject.Address, BalanceChangeType.Credit, this.subject.Id);

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithEqualsAddressAndBalanceChangeType_ShouldReturnTrue()
        {
            var other = new AddressRule(this.subject.Address, this.subject.BalanceChangeType, this.subject.Id);

            Assert.True(this.subject.Equals(other));
        }
    }
}
