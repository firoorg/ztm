using System;
using NBitcoin;
using Xunit;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class TransactionWatchTests
    {
        [Fact]
        public void Constructor_WithNullTxId_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("txId", () => new TransactionWatch<object>(null, uint256.One, null));
            Assert.Throws<ArgumentNullException>("txId", () => new TransactionWatch<object>(null, uint256.One, null, DateTime.Now));
            Assert.Throws<ArgumentNullException>("txId", () => new TransactionWatch<object>(null, uint256.One, null, DateTime.Now, Guid.NewGuid()));
        }

        [Fact]
        public void TransactionId_WhenConstructed_ShouldSameAsConstructorArg()
        {
            var subject1 = new TransactionWatch<object>(null, uint256.One, uint256.One);
            var subject2 = new TransactionWatch<object>(null, uint256.One, uint256.One, DateTime.Now);
            var subject3 = new TransactionWatch<object>(null, uint256.One, uint256.One, DateTime.Now, Guid.NewGuid());

            Assert.Equal(uint256.One, subject1.TransactionId);
            Assert.Equal(uint256.One, subject2.TransactionId);
            Assert.Equal(uint256.One, subject3.TransactionId);
        }
    }
}
