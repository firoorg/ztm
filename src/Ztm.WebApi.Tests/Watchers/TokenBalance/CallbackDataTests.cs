using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Watchers.TokenBalance;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenBalance
{
    public sealed class CallbackDataTests
    {
        readonly CallbackData subject;

        public CallbackDataTests()
        {
            this.subject = new CallbackData()
            {
                Confirmation = 6,
                Received = new PropertyAmount(100),
            };
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(
                this.subject,
                s => new CallbackData() { Confirmation = s.Confirmation, Received = s.Received });

            Assert.DoesNotContain(false, results);
        }

        [Fact]
        public void Equals_WithUnequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(
                this.subject,
                s => new CallbackData() { Confirmation = null, Received = s.Received },
                s => new CallbackData() { Confirmation = 0, Received = s.Received },
                s => new CallbackData() { Confirmation = s.Confirmation, Received = null },
                s => new CallbackData() { Confirmation = s.Confirmation, Received = new PropertyAmount(0) });

            Assert.DoesNotContain(true, results);
        }
    }
}
