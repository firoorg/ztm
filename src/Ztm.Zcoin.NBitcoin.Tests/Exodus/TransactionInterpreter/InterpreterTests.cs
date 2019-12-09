using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Tests;
using Ztm.Zcoin.NBitcoin.Tests.Exodus;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public class InterpreterTests
    {
        readonly IExodusInterpreter defaultInterpreter;
        readonly IEnumerable<IExodusInterpreter> transactionInterpreters;
        readonly Interpreter subject;
        public InterpreterTests()
        {
            this.defaultInterpreter = Substitute.For<IExodusInterpreter>();
            this.defaultInterpreter.SupportType.Returns(typeof(FakeExodusTransaction));

            this.transactionInterpreters = new Collection<IExodusInterpreter>
            {
                this.defaultInterpreter,
            };

            this.subject = new Interpreter(this.transactionInterpreters);
        }

        [Fact]
        public void Construct_WithNullArgs_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "transactionInterpreters",
                () => new Interpreter(null)
            );
        }

        [Fact]
        public void Interpret_WithNullTransaction_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "transaction",
                () => this.subject.Interpret(null)
            );
        }

        [Fact]
        public void Interpret_WithoutExodusData_ShouldThrow()
        {
            var tx =  Transaction.Parse(
                ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);

            Assert.Throws<ArgumentException>(
                "transaction",
                () => this.subject.Interpret(tx)
            );
        }

        [Fact]
        public void Interpret_WithUnsupportedExodusTransactionType_ShouldThrow()
        {
            var tx =  Transaction.Parse(
                ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);
            var unsupported = new FakeUnsupportedExodusTransaction(TestAddress.Regtest1, TestAddress.Regtest2);

            #pragma warning disable CS0618
            tx.SetExodusTransaction(unsupported);
            #pragma warning restore CS0618

            Assert.Throws<TransactionFieldException>(
                () => this.subject.Interpret(tx)
            );
        }

        [Fact]
        public void Interpret_WithSupportedType_ShouldSuccess()
        {
            // Arrange.
            var tx =  Transaction.Parse(
                ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);
            var exodus = new FakeExodusTransaction(TestAddress.Regtest1, TestAddress.Regtest2);

            #pragma warning disable CS0618
            tx.SetExodusTransaction(exodus);
            #pragma warning restore CS0618

            var address = TestAddress.Mainnet1;
            var amount = new PropertyAmount(10);
            var property = new PropertyId(2);

            var changes = new Collection<BalanceChange>
            {
                new BalanceChange(address, amount, property)
            };

            this.defaultInterpreter.Interpret(Arg.Is<ExodusTransaction>(t => exodus == t)).Returns(changes);

            // Act.
            var retrievedChanges = this.subject.Interpret(tx);

            // Assert.
            this.defaultInterpreter.Received(1).Interpret(Arg.Is<ExodusTransaction>(t => exodus == t));
            Assert.Equal(changes, retrievedChanges);
        }
    }

    class FakeUnsupportedExodusTransaction : ExodusTransaction
    {
        public FakeUnsupportedExodusTransaction(BitcoinAddress sender, BitcoinAddress receiver) : base(sender, receiver)
        {
        }

        public override int Id => throw new NotImplementedException();

        public override int Version => throw new NotImplementedException();
    }
}