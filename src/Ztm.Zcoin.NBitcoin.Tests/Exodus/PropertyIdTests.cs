using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class PropertyIdTests
    {
        readonly PropertyId subject;

        public PropertyIdTests()
        {
            this.subject = new PropertyId(1);
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        [InlineData((long)uint.MaxValue + 1)]
        public void Constructor_WithInvalidValue_ShouldThrow(long value)
        {
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new PropertyId(value));
        }

        [Theory]
        [InlineData(1L)]
        [InlineData((long)uint.MaxValue)]
        public void Constructor_WithValidValue_ShoulSuccess(long value)
        {
            var id = new PropertyId(value);

            Assert.Equal(value, id.Value);
        }

        [Fact]
        public void Parse_WithNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("s", () => PropertyId.Parse(null));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("4294967295")]
        public void Parse_WithValidString_ShouldSuccess(string value)
        {
            var id = PropertyId.Parse(value);

            Assert.Equal(value, id.ToString());
        }

        [Theory]
        [InlineData("a")]
        [InlineData("1.0")]
        [InlineData("-9223372036854775809")]
        [InlineData("9223372036854775808")]
        [InlineData("-1")]
        [InlineData("0")]
        [InlineData("4294967296")]
        public void Parse_WithInvalidString_ShouldThrow(string value)
        {
            Assert.Throws<FormatException>(() => PropertyId.Parse(value));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(this.subject.Value));
        }

        [Fact]
        public void Equals_WithDifferentValue_ShouldReturnFalse()
        {
            var other = new PropertyId(2);

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithSameValue_ShouldReturnTrue()
        {
            var other = new PropertyId(this.subject.Value);

            Assert.True(this.subject.Equals(other));
        }

        [Fact]
        public void ToString_WhenInvoke_ShouldReturnStringRepresentValue()
        {
            Assert.Equal(this.subject.Value.ToString(), this.subject.ToString());
        }

        [Fact]
        public void Equality_WithBothNull_ShouldReturnTrue()
        {
            PropertyId first = null, second = null;

            Assert.True(first == second);
        }

        [Fact]
        public void Equality_WithFirstNull_ShouldReturnFalse()
        {
            Assert.False(null == this.subject);
        }

        [Fact]
        public void Equality_WithSecondNull_ShouldReturnFalse()
        {
            Assert.False(this.subject == null);
        }

        [Fact]
        public void Equality_WithDifferentValue_ShouldReturnFalse()
        {
            var other = new PropertyId(2);

            Assert.False(this.subject == other);
        }

        [Fact]
        public void Equality_WithSameValue_ShouldReturnTrue()
        {
            var other = new PropertyId(this.subject.Value);

            Assert.True(this.subject == other);
        }

        [Fact]
        public void Inequality_WithBothNull_ShouldReturnFalse()
        {
            PropertyId first = null, second = null;

            Assert.False(first != second);
        }

        [Fact]
        public void Inequality_WithFirstNull_ShouldReturnTrue()
        {
            Assert.True(null != this.subject);
        }

        [Fact]
        public void Inequality_WithSecondNull_ShouldReturnTrue()
        {
            Assert.True(this.subject != null);
        }

        [Fact]
        public void Inequality_WithSameInstance_ShouldReturnFalse()
        {
            #pragma warning disable CS1718
            Assert.False(this.subject != this.subject);
            #pragma warning restore CS1718
        }

        [Fact]
        public void Inequality_WithSameValue_ShouldReturnFalse()
        {
            var other = new PropertyId(this.subject.Value);

            Assert.False(this.subject != other);
        }

        [Fact]
        public void Inequality_WithDifferentValue_ShouldReturnTrue()
        {
            var other = new PropertyId(2);

            Assert.True(this.subject != other);
        }
    }
}
