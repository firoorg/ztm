using NBitcoin;
using Xunit;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class ExodusTransactionTests
    {
        [Fact]
        public void Constructor_WithNullSender_ShouldSuccess()
        {
            var tx = new TestExodusTransaction(null, null);

            Assert.Null(tx.Sender);
        }

        [Fact]
        public void Constructor_WithNonNullSender_ShouldSuccess()
        {
            var sender = BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", ZcoinNetworks.Instance.Regtest);
            var tx = new TestExodusTransaction(sender, null);

            Assert.Same(sender, tx.Sender);
        }

        [Fact]
        public void Constructor_WithNullReceiver_ShouldSuccess()
        {
            var tx = new TestExodusTransaction(null, null);

            Assert.Null(tx.Receiver);
        }

        [Fact]
        public void Constructor_WithNonNullReceiver_ShouldSuccess()
        {
            var receiver = BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", ZcoinNetworks.Instance.Regtest);
            var tx = new TestExodusTransaction(null, receiver);

            Assert.Same(receiver, tx.Receiver);
        }
    }
}
