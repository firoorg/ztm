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
                Block = new Block()
                {
                    Height = 1
                },
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
                Block = new Block()
                {
                    Height = 2
                }
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                Block = new Block()
                {
                    Height = 0
                }
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsEqualButIndexIsGreater_ShouldLower()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                Block = new Block()
                {
                    Height = 1
                },
                Index = 2
            });

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsEqualButIndexIsLower_ShouldGreater()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                Block = new Block()
                {
                    Height = 1
                },
                Index = 0
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_OtherBlockIsEqualAndIndexIsEqual_ShouldEqual()
        {
            var result = this.subject.CompareTo(new BlockTransaction()
            {
                Block = new Block()
                {
                    Height = 1
                },
                Index = 1
            });

            Assert.Equal(0, result);
        }
    }
}
