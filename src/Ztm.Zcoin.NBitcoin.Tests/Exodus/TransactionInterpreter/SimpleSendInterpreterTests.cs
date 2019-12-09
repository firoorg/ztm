using System;
using System.Linq;
using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus.TransactionInterpreter
{
    public sealed class SimpleSendInterpreterTests
    {
        readonly SimpleSendInterpreter subject;

        public SimpleSendInterpreterTests()
        {
            this.subject = new SimpleSendInterpreter();
        }

        [Fact]
        public void SupportType_ShouldReturnSimpleSendV0()
        {
            Assert.Equal(typeof(SimpleSendV0), this.subject.SupportType);
        }

        [Fact]
        public void Interpret_WithNullExodusTransaction_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "transaction",
                () => this.subject.Interpret(null)
            );
        }

        [Fact]
        public void Interpret_WithSimepleSend_ShouldReturnBalanceChanges()
        {
            // Arrange.
            var sender = TestAddress.Regtest1;
            var receiver = TestAddress.Regtest2;
            var property = new PropertyId(2);
            var amount = new PropertyAmount(100);

            var tx = new SimpleSendV0(sender, receiver, property, amount);

            // Act.
            var changes = this.subject.Interpret(tx);

            // Assert.
            Assert.NotNull(changes);
            Assert.Equal(2, changes.Count());
            Assert.Contains(new BalanceChange(sender, PropertyAmount.Negate(amount), property), changes);
            Assert.Contains(new BalanceChange(receiver, amount, property), changes);
        }
    }
}