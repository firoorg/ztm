using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class CallbackAmountTests
    {
        readonly CallbackAmount subject;

        public CallbackAmountTests()
        {
            this.subject = new CallbackAmount()
            {
                Confirmed = new PropertyAmount(100),
                Pending = new PropertyAmount(200),
            };
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(
                this.subject,
                s => new CallbackAmount() { Confirmed = s.Confirmed, Pending = s.Pending });

            Assert.DoesNotContain(false, results);
        }

        [Fact]
        public void Equals_WithUnequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(
                this.subject,
                s => new CallbackAmount() { Confirmed = null, Pending = s.Pending },
                s => new CallbackAmount() { Confirmed = new PropertyAmount(200), Pending = s.Pending },
                s => new CallbackAmount() { Confirmed = s.Confirmed, Pending = null },
                s => new CallbackAmount() { Confirmed = s.Confirmed, Pending = new PropertyAmount(100) });

            Assert.DoesNotContain(true, results);
        }

        [Fact]
        public void Equality_WithBothNull_ShouldReturnTrue()
        {
            CallbackAmount left, right;

            left = null;
            right = null;

            Assert.True(left == right);
        }

        [Fact]
        public void Equality_WithLeftNull_ShouldReturnFalse()
        {
            CallbackAmount left = null;

            Assert.False(left == this.subject);
        }

        [Fact]
        public void Equality_WithEqual_ShouldReturnTrue()
        {
            var right = new CallbackAmount()
            {
                Confirmed = this.subject.Confirmed,
                Pending = this.subject.Pending,
            };

            Assert.True(this.subject == right);
        }

        [Fact]
        public void Equality_WithUnequal_ShouldReturnFalse()
        {
            var s = this.subject;

            Assert.False(this.subject == new CallbackAmount() { Confirmed = null, Pending = s.Pending });
            Assert.False(this.subject == new CallbackAmount() { Confirmed = s.Pending, Pending = s.Pending });
            Assert.False(this.subject == new CallbackAmount() { Confirmed = s.Confirmed, Pending = null });
            Assert.False(this.subject == new CallbackAmount() { Confirmed = s.Confirmed, Pending = s.Confirmed });
        }

        [Fact]
        public void Inequality_WithBothNull_ShouldReturnFalse()
        {
            CallbackAmount left, right;

            left = null;
            right = null;

            Assert.False(left != right);
        }

        [Fact]
        public void Inequality_WithLeftNull_ShouldReturnTrue()
        {
            CallbackAmount left = null;

            Assert.True(left != this.subject);
        }

        [Fact]
        public void Inequality_WithEqual_ShouldReturnFalse()
        {
            var right = new CallbackAmount()
            {
                Confirmed = this.subject.Confirmed,
                Pending = this.subject.Pending,
            };

            Assert.False(this.subject != right);
        }

        [Fact]
        public void Inequality_WithUnequal_ShouldReturnTrue()
        {
            var s = this.subject;

            Assert.True(this.subject != new CallbackAmount() { Confirmed = null, Pending = s.Pending });
            Assert.True(this.subject != new CallbackAmount() { Confirmed = s.Pending, Pending = s.Pending });
            Assert.True(this.subject != new CallbackAmount() { Confirmed = s.Confirmed, Pending = null });
            Assert.True(this.subject != new CallbackAmount() { Confirmed = s.Confirmed, Pending = s.Confirmed });
        }
    }
}
