using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Watchers.TokenBalance;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class TimeoutDataTests
    {
        readonly TimeoutData subject;

        public TimeoutDataTests()
        {
            this.subject = new TimeoutData()
            {
                Confirmation = 3,
                Received = new PropertyAmount(100),
                TargetConfirmation = 6,
            };
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(
                this.subject,
                s => new TimeoutData()
                {
                    Confirmation = s.Confirmation,
                    Received = s.Received,
                    TargetConfirmation = s.TargetConfirmation
                });

            Assert.DoesNotContain(false, results);
        }

        [Fact]
        public void Equals_WithUnequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(
                this.subject,
                s => new TimeoutData() { Confirmation = null, Received = s.Received },
                s => new TimeoutData() { Confirmation = 0, Received = s.Received },
                s => new TimeoutData() { Confirmation = s.Confirmation, Received = null },
                s => new TimeoutData() { Confirmation = s.Confirmation, Received = new PropertyAmount(0) },
                s => new TimeoutData()
                {
                    Confirmation = s.Confirmation,
                    Received = s.Received,
                    TargetConfirmation = 0
                });

            Assert.DoesNotContain(true, results);
        }
    }
}
