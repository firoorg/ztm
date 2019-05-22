using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Tests.Contexts.Main
{
    public class OutputTests
    {
        readonly Output subject;

        public OutputTests()
        {
            this.subject = new Output()
            {
                TransactionHash = uint256.One,
                Index = 1
            };
        }

        [Fact]
        public void CompareTo_OtherIsNull_ShouldGreater()
        {
            var result = this.subject.CompareTo(null);

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherTransactionHashIsGreater_ShouldLower()
        {
            var result = this.subject.CompareTo(new Output()
            {
                TransactionHash = new uint256(2)
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherTransactionHashIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new Output()
            {
                TransactionHash = uint256.Zero
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherTransactionHashIsEqualButIndexIsGreater_ShouldLower()
        {
            var result = this.subject.CompareTo(new Output()
            {
                TransactionHash = uint256.One,
                Index = 2
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherTransactionHashIsEqualButIndexIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new Output()
            {
                TransactionHash = uint256.One,
                Index = 0
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherTransactionHashIsEqualAndIndexIsEqual_ShouldEqual()
        {
            var result = this.subject.CompareTo(new Output()
            {
                TransactionHash = uint256.One,
                Index = 1
            });

            Assert.Equal(0, result);
        }
    }
}
