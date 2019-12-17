using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class ExodusTransactionTests
    {
        [Fact]
        public void Constructor_WithNullSender_ShouldSuccess()
        {
            var tx = new FakeExodusTransaction(null, null);

            Assert.Null(tx.Sender);
        }

        [Fact]
        public void Constructor_WithNonNullSender_ShouldSuccess()
        {
            var sender = BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", ZcoinNetworks.Instance.Regtest);
            var tx = new FakeExodusTransaction(sender, null);

            Assert.Same(sender, tx.Sender);
        }

        [Fact]
        public void Constructor_WithNullReceiver_ShouldSuccess()
        {
            var tx = new FakeExodusTransaction(null, null);

            Assert.Null(tx.Receiver);
        }

        [Fact]
        public void Constructor_WithNonNullReceiver_ShouldSuccess()
        {
            var receiver = BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", ZcoinNetworks.Instance.Regtest);
            var tx = new FakeExodusTransaction(null, receiver);

            Assert.Same(receiver, tx.Receiver);
        }

        [Theory]
        [InlineData(ExodusTransaction.MinId)]
        [InlineData(ExodusTransaction.MaxId)]
        public void IsValidId_WithValidId_ShouldReturnTrue(int id)
        {
            Assert.True(ExodusTransaction.IsValidId(id));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void IsValidId_WithInvalidId_ShouldReturnFalse(int id)
        {
            Assert.False(ExodusTransaction.IsValidId(id));
        }

        [Theory]
        [InlineData(ExodusTransaction.MinVersion)]
        [InlineData(ExodusTransaction.MaxVersion)]
        public void IsValidVersion_WithValidVersion_ShouldReturnTrue(int version)
        {
            Assert.True(ExodusTransaction.IsValidVersion(version));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void IsValidVersion_WithInvalidVersion_ShouldReturnFalse(int version)
        {
            Assert.False(ExodusTransaction.IsValidVersion(version));
        }
    }
}
