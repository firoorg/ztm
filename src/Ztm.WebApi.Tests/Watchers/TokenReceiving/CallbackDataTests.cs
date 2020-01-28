using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class CallbackDataTests
    {
        readonly CallbackData subject;

        public CallbackDataTests()
        {
            this.subject = new CallbackData()
            {
                Received = new CallbackAmount()
                {
                    Confirmed = new PropertyAmount(100),
                },
            };
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(this.subject, s => new CallbackData() { Received = s.Received });

            Assert.DoesNotContain(false, results);
        }

        [Fact]
        public void Equals_WithUnequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(
                this.subject,
                s => new CallbackData() { Received = null },
                s => new CallbackData() { Received = new CallbackAmount() { Pending = new PropertyAmount(100) } });

            Assert.DoesNotContain(true, results);
        }
    }
}
