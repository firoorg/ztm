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
        public void CanInterpret_WithNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "type",
                () => this.subject.CanInterpret(null)
            );
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(ExodusTransaction))]
        public void CanInterpret_WithInvalidType_ShouldReturnFalse(Type type)
        {
            Assert.False(this.subject.CanInterpret(type));
        }

        [Fact]
        public void CanInterpret_WithSimpleSendV0_ShouldSuccess()
        {
            Assert.True(this.subject.CanInterpret(typeof(SimpleSendV0)));
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