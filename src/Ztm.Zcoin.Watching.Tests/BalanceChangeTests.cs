using System;
using FluentAssertions;
using Xunit;
using Ztm.Testing;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class BalanceChangeTests
    {
        readonly object context;
        readonly BalanceChange<object, int> subject;

        public BalanceChangeTests()
        {
            this.context = new object();
            this.subject = new BalanceChange<object, int>(this.context, 10);
        }

        [Fact]
        public void Constructor_WithNullAmount_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "amount",
                () => new BalanceChange<object, object>(null, null)
            );
        }

        [Fact]
        public void Constructor_WithValidArgs_ShouldInitializeProperties()
        {
            Assert.Equal(10, this.subject.Amount);
            Assert.Same(this.context, this.subject.Context);
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(
                this.subject,
                s => new BalanceChange<object, int>(null, s.Amount),
                s => new BalanceChange<object, int>(new object(), s.Amount));

            results.Should().NotContain(false);
        }

        [Fact]
        public void Equals_WithInequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(
                this.subject,
                s => new BalanceChange<object, int>(s.Context, 11));

            results.Should().NotContain(true);
        }
    }
}
