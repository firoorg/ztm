using Xunit;

namespace Ztm.ObjectModel.Tests
{
    public class ReferenceEqualityComparerTests
    {
        readonly NotEqualObject obj;

        public ReferenceEqualityComparerTests()
        {
            this.obj = new NotEqualObject();
        }

        [Fact]
        public void Equals_WithBothNull_ShouldReturnTrue()
        {
            Assert.True(ReferenceEqualityComparer.Default.Equals(null, null));
            Assert.True(ReferenceEqualityComparer<NotEqualObject>.Default.Equals(null, null));
        }

        [Fact]
        public void Equals_WithFirstNull_ShouldReturnFalse()
        {
            Assert.False(ReferenceEqualityComparer.Default.Equals(null, this.obj));
            Assert.False(ReferenceEqualityComparer<NotEqualObject>.Default.Equals(null, this.obj));
        }

        [Fact]
        public void Equals_WithSecondNull_ShouldReturnFalse()
        {
            Assert.False(ReferenceEqualityComparer.Default.Equals(this.obj, null));
            Assert.False(ReferenceEqualityComparer<NotEqualObject>.Default.Equals(this.obj, null));
        }

        [Fact]
        public void Equals_WithSameInstance_ShouldReturnTrue()
        {
            Assert.True(ReferenceEqualityComparer.Default.Equals(this.obj, this.obj));
            Assert.True(ReferenceEqualityComparer<NotEqualObject>.Default.Equals(this.obj, this.obj));
        }

        [Fact]
        public void Equals_WithDiffrentInstance_ShouldReturnFalse()
        {
            var first = new EqualObject();
            var second = new EqualObject();

            Assert.False(ReferenceEqualityComparer.Default.Equals(first, second));
            Assert.False(ReferenceEqualityComparer<EqualObject>.Default.Equals(first, second));
        }

        [Fact]
        public void GetHashCode_WithNullTwoTime_ShouldEqual()
        {
            Assert.Equal(
                ReferenceEqualityComparer.Default.GetHashCode(null),
                ReferenceEqualityComparer.Default.GetHashCode(null)
            );
            Assert.Equal(
                ReferenceEqualityComparer<NotEqualObject>.Default.GetHashCode(null),
                ReferenceEqualityComparer<NotEqualObject>.Default.GetHashCode(null)
            );
        }

        [Fact]
        public void GetHashCode_WithNullAndNonNull_ShouldNotEqual()
        {
            Assert.NotEqual(
                ReferenceEqualityComparer.Default.GetHashCode(null),
                ReferenceEqualityComparer.Default.GetHashCode(this.obj)
            );
            Assert.NotEqual(
                ReferenceEqualityComparer<NotEqualObject>.Default.GetHashCode(null),
                ReferenceEqualityComparer<NotEqualObject>.Default.GetHashCode(this.obj)
            );
        }

        [Fact]
        public void GetHashCode_WithSameInstance_ShouldEqual()
        {
            Assert.Equal(
                ReferenceEqualityComparer.Default.GetHashCode(this.obj),
                ReferenceEqualityComparer.Default.GetHashCode(this.obj)
            );
            Assert.Equal(
                ReferenceEqualityComparer<NotEqualObject>.Default.GetHashCode(this.obj),
                ReferenceEqualityComparer<NotEqualObject>.Default.GetHashCode(this.obj)
            );
        }

        [Fact]
        public void GetHashCode_WithDifferentInstance_ShouldNotEqual()
        {
            var first = new EqualObject();
            var second = new EqualObject();

            Assert.NotEqual(
                ReferenceEqualityComparer.Default.GetHashCode(first),
                ReferenceEqualityComparer.Default.GetHashCode(second)
            );
            Assert.NotEqual(
                ReferenceEqualityComparer<EqualObject>.Default.GetHashCode(first),
                ReferenceEqualityComparer<EqualObject>.Default.GetHashCode(second)
            );
        }

        class EqualObject
        {
            public override bool Equals(object obj)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 999;
            }
        }

        class NotEqualObject
        {
            int hash;

            public override bool Equals(object obj)
            {
                return false;
            }

            public override int GetHashCode()
            {
                return hash++;
            }
        }
    }
}
