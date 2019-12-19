using Xunit;
using Ztm.Data.Entity.Contexts.Main;
using uint256 = NBitcoin.uint256;

namespace Ztm.Data.Entity.Tests.Contexts.Main
{
    public class BlockTests
    {
        readonly Block subject;

        public BlockTests()
        {
            this.subject = new Block()
            {
                Height = 1,
                Hash = uint256.One
            };
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitTransactionsWithEmpty()
        {
            Assert.NotNull(this.subject.Transactions);
            Assert.Empty(this.subject.Transactions);
        }

        [Fact]
        public void CompareTo_OtherIsNull_ShouldGreater()
        {
            var result = this.subject.CompareTo(null);

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherHeightIsGreater_ShouldLower()
        {
            var result = this.subject.CompareTo(new Block()
            {
                Height = 2,
                Hash = this.subject.Hash
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherHeightIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new Block()
            {
                Height = 0,
                Hash = this.subject.Hash
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherSameHeightAndHashIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new Block()
            {
                Height = 1,
                Hash = uint256.Zero
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherSameHeightAndHashIsGreater_ShouldLower()
        {
            var result = this.subject.CompareTo(new Block()
            {
                Height = 1,
                Hash = uint256.Parse("0000000000000000000000000000000000000000000000000000000000000002")
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherSameHeightAndSameHash_ShouldEqual()
        {
            var result = this.subject.CompareTo(new Block()
            {
                Height = this.subject.Height,
                Hash = this.subject.Hash
            });

            Assert.Equal(0, result);
        }

        [Fact]
        public void CompareTo_OtherSameHeightAndHashIsNull_ShouldLower()
        {
            var subject = new Block
            {
                Height = 1,
                Hash = null
            };

            var result = subject.CompareTo(new Block()
            {
                Height = 1,
                Hash = uint256.Zero
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_WithOtherType_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(string.Empty));
        }

        [Fact]
        public void Equals_WithDifferenceValue_ShouldReturnFalse()
        {
            Block other;

            other = new Block
            {
                Height = 2,
                Hash = this.subject.Hash
            };

            Assert.False(this.subject.Equals(other));

            other = new Block
            {
                Height = this.subject.Height,
                Hash = uint256.Zero
            };

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithSameValue_ShouldReturnTrue()
        {
            var other = new Block
            {
                Height = this.subject.Height,
                Hash = this.subject.Hash
            };

            Assert.True(this.subject.Equals(other));
        }

        [Fact]
        public void GetHashCode_WithSameValue_ShouldGetSameHashCode()
        {
            // Arrange.
            var other = new Block
            {
                Height = this.subject.Height,
                Hash = this.subject.Hash
            };

            // Act.
            var newHash = other.GetHashCode();

            // Assert.
            Assert.Equal(this.subject.GetHashCode(), newHash);
        }

        [Fact]
        public void GetHashCode_WithDifferenceValue_ItIsHighlyPossibleThatGetDifferenceValues()
        {
            Block other;

            other = new Block
            {
                Height = 2,
                Hash = this.subject.Hash
            };

            Assert.NotEqual(this.subject.GetHashCode(), other.GetHashCode());

            other = new Block
            {
                Height = this.subject.Height,
                Hash = uint256.Zero
            };

            Assert.NotEqual(this.subject.GetHashCode(), other.GetHashCode());
        }
    }
}
