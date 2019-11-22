using NBitcoin;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public sealed class TransactionExtensionsTests
    {
        readonly Transaction subject;

        public TransactionExtensionsTests()
        {
            this.subject = Transaction.Parse(ZcoinTransactionData.ZerocoinRemint, ZcoinNetworks.Instance.Regtest);
        }

        [Fact]
        public void SetExodusTransaction_WithNull_ShouldNotThrow()
        {
            #pragma warning disable CS0618
            this.subject.SetExodusTransaction(null);
            #pragma warning restore CS0618
        }

        [Fact]
        public void GetExodusTransaction_WithUnsetValue_ShouldRetreiveNull()
        {
            Assert.Null(this.subject.GetExodusTransaction());
        }

        [Fact]
        public void SetAndGetExodusTransaction_WithValidTransaction_ShouldRetreiveSameTx()
        {
            // Arrange.
            var address = TestAddress.Regtest1;
            var propertyId = new PropertyId(3);
            var amount = new PropertyAmount(10);

            var exodusTx = new SimpleSendV0(address, address, propertyId, amount);

            // Act.
            #pragma warning disable CS0618
            this.subject.SetExodusTransaction(exodusTx);
            #pragma warning restore CS0618
            var retrieved = this.subject.GetExodusTransaction();

            // Assert.
            Assert.Same(exodusTx, retrieved);
        }
    }
}