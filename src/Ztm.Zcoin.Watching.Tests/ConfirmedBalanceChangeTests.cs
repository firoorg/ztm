using System;
using Xunit;
using Ztm.Testing;

namespace Ztm.Zcoin.Watching.Tests
{
    public sealed class ConfirmedBalanceChangeTests
    {
        readonly object context;
        readonly ConfirmedBalanceChange<object, int> subject;

        public ConfirmedBalanceChangeTests()
        {
            this.context = new object();
            this.subject = new ConfirmedBalanceChange<object, int>(this.context, 10, 1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidConfirmation_ShouldThrow(int confirmation)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                "confirmation",
                () => new ConfirmedBalanceChange<object, int>(null, 10, confirmation)
            );
        }

        [Fact]
        public void Constructor_WhenSuccess_ShouldInitializeProperties()
        {
            Assert.Equal(this.context, this.subject.Context);
            Assert.Equal(10, this.subject.Amount);
            Assert.Equal(1, this.subject.Confirmation);
        }

        [Fact]
        public void Equals_WithEqual_ShouldBeEqual()
        {
            EqualityTesting.TestEquals(
                this.subject,
                s => new ConfirmedBalanceChange<object, int>(null, s.Amount, s.Confirmation)
            );
        }

        [Fact]
        public void Equals_WithUnequal_ShouldBeUnequal()
        {
            EqualityTesting.TestInequal(
                this.subject,
                s => new ConfirmedBalanceChange<object, int>(s.Context, 9, s.Confirmation),
                s => new ConfirmedBalanceChange<object, int>(s.Context, s.Amount, 2)
            );
        }
    }
}
