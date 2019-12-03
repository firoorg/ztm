using System;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class PropertyTests
    {
        readonly Property subject;

        public PropertyTests()
        {
            this.subject = new Property(new PropertyId(1), PropertyType.Divisible);
        }

        [Fact]
        public void Constructor_WithNullId_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("id", () => new Property(null, PropertyType.Indivisible));
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitProperties()
        {
            Assert.Equal(this.subject.Id, new PropertyId(1));
            Assert.Equal(PropertyType.Divisible, this.subject.Type);
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            Assert.False(this.subject.Equals(this.subject.Id));
        }

        [Fact]
        public void Equals_WithDifferentId_ShouldReturnFalse()
        {
            var other = new Property(new PropertyId(2), this.subject.Type);

            Assert.False(this.subject.Equals(other));
        }

        [Fact]
        public void Equals_WithSameId_ShouldReturnTrue()
        {
            var other = new Property(new PropertyId(1), PropertyType.Indivisible);

            Assert.True(this.subject.Equals(other));
        }
    }
}
