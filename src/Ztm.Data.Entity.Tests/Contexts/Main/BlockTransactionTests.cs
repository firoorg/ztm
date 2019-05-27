using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Tests.Contexts.Main
{
    public class BlockTransactionTests
    {
        readonly BlockTransaction subject;

        public BlockTransactionTests()
        {
            this.subject = new BlockTransaction()
            {
                BlockHash = uint256.One,
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
        public void CompareTo_OtherBlockIsGreater_ShouldLower()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                BlockHash = new uint256(2)
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                BlockHash = uint256.Zero
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsEqualButIndexIsGreater_ShouldLower()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                BlockHash = uint256.One,
                Index = 2
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsEqualButIndexIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                BlockHash = uint256.One,
                Index = 0
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsEqualAndIndexIsEqual_ShouldEqual()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                BlockHash = uint256.One,
                Index = 1
            });

            Assert.Equal(0, result);
        }
    }
}
