using System;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus.TransactionInterpreter
{
    public sealed class BalanceChangeTests
    {
        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            var address = TestAddress.Mainnet1;
            var amount = new PropertyAmount(10);
            var property = new PropertyId(2);

            Assert.Throws<ArgumentNullException>(
                "address",
                () => new BalanceChange(null, amount, property)
            );

            Assert.Throws<ArgumentNullException>(
                "property",
                () => new BalanceChange(address, amount, null)
            );
        }

        [Fact]
        public void Construct_WithValidArgs_ShouldSuccess()
        {
            // Arrange.
            var address = TestAddress.Mainnet1;
            var amount = new PropertyAmount(10);
            var property = new PropertyId(2);

            // Act.
            var c = new BalanceChange(address, amount, property);

            // Assert.
            Assert.Equal(address, c.Address);
            Assert.Equal(amount, c.Amount);
            Assert.Equal(property, c.Property);
        }
    }
}