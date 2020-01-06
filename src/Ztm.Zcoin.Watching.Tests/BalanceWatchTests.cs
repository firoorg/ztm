using System;
using NBitcoin;
using Xunit;
using Ztm.Testing;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class BalanceWatchTests
    {
        readonly object context;
        readonly BalanceWatch<object, int> subject;

        public BalanceWatchTests()
        {
            this.context = new object();
            this.subject = new BalanceWatch<object, int>(
                this.context,
                TestBlock.Regtest0.GetHash(),
                uint256.One,
                TestAddress.Regtest1,
                10
            );
        }

        [Fact]
        public void Constructor_WithNullTx_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "tx",
                () => new BalanceWatch<object, int>(
                    null,
                    uint256.One,
                    null,
                    TestAddress.Regtest1,
                    1
                )
            );

            Assert.Throws<ArgumentNullException>(
                "tx",
                () => new BalanceWatch<object, int>(
                    null,
                    uint256.One,
                    null,
                    TestAddress.Regtest1,
                    1,
                    DateTime.Now
                )
            );

            Assert.Throws<ArgumentNullException>(
                "tx",
                () => new BalanceWatch<object, int>(
                    null,
                    uint256.One,
                    null,
                    TestAddress.Regtest1,
                    1,
                    DateTime.Now,
                    Guid.NewGuid()
                )
            );
        }

        [Fact]
        public void Constructor_WithNullAddress_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "address",
                () => new BalanceWatch<object, int>(
                    null,
                    uint256.One,
                    uint256.One,
                    null,
                    1
                )
            );

            Assert.Throws<ArgumentNullException>(
                "address",
                () => new BalanceWatch<object, int>(
                    null,
                    uint256.One,
                    uint256.One,
                    null,
                    1,
                    DateTime.Now
                )
            );

            Assert.Throws<ArgumentNullException>(
                "address",
                () => new BalanceWatch<object, int>(
                    null,
                    uint256.One,
                    uint256.One,
                    null,
                    1,
                    DateTime.Now,
                    Guid.NewGuid()
                )
            );
        }

        [Fact]
        public void Constructor_WithNullBalanceChange_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "balanceChange",
                () => new BalanceWatch<object, object>(
                    null,
                    uint256.One,
                    uint256.One,
                    TestAddress.Regtest1,
                    null
                )
            );

            Assert.Throws<ArgumentNullException>(
                "balanceChange",
                () => new BalanceWatch<object, object>(
                    null,
                    uint256.One,
                    uint256.One,
                    TestAddress.Regtest1,
                    null,
                    DateTime.Now
                )
            );

            Assert.Throws<ArgumentNullException>(
                "balanceChange",
                () => new BalanceWatch<object, object>(
                    null,
                    uint256.One,
                    uint256.One,
                    TestAddress.Regtest1,
                    null,
                    DateTime.Now,
                    Guid.NewGuid()
                )
            );
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            Assert.Equal(TestAddress.Regtest1, this.subject.Address);
            Assert.Equal(10, this.subject.BalanceChange);
            Assert.Equal(uint256.One, this.subject.Transaction);
        }

        [Fact]
        public void Equals_WithEqual_ShouldBeEqual()
        {
            EqualityTesting.TestEquals(
                this.subject,
                s => new BalanceWatch<object, int>(null, s.StartBlock, s.Transaction, s.Address, s.BalanceChange, s.StartTime, s.Id),
                s => new BalanceWatch<object, int>(s.Context, s.StartBlock, s.Transaction, s.Address, 9, s.StartTime, s.Id),
                s => new BalanceWatch<object, int>(s.Context, s.StartBlock, s.Transaction, s.Address, s.BalanceChange, DateTime.MinValue, s.Id),
                s => new BalanceWatch<object, int>(s.Context, s.StartBlock, s.Transaction, s.Address, s.BalanceChange, s.StartTime, Guid.NewGuid())
            );
        }

        [Fact]
        public void Equals_WithUnequal_ShouldBeUnequal()
        {
            EqualityTesting.TestInequal(
                this.subject,
                s => new BalanceWatch<object, int>(s.Context, uint256.Zero, s.Transaction, s.Address, s.BalanceChange, s.StartTime, s.Id),
                s => new BalanceWatch<object, int>(s.Context, s.StartBlock, uint256.Zero, s.Address, s.BalanceChange, s.StartTime, s.Id),
                s => new BalanceWatch<object, int>(s.Context, s.StartBlock, s.Transaction, TestAddress.Regtest2, s.BalanceChange, s.StartTime, s.Id)
            );
        }
    }
}
