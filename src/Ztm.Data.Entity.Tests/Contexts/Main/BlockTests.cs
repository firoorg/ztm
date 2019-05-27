using Xunit;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Tests.Contexts.Main
{
    public class BlockTests
    {
        readonly Block subject;

        public BlockTests()
        {
            this.subject = new Block()
            {
                Height = 1
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
                Height = 2
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherHeightIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new Block()
            {
                Height = 0
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherHeightIsEqual_ShouldEqual()
        {
            var result = this.subject.CompareTo(new Block()
            {
                Height = 1
            });

            Assert.Equal(0, result);
        }
    }
}
