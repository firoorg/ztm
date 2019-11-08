using System;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class SimpleSendV0Tests
    {
        readonly BitcoinAddress sender;
        readonly BitcoinAddress receiver;
        readonly PropertyId property;
        readonly PropertyAmount amount;

        public SimpleSendV0Tests()
        {
            this.sender = BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", ZcoinNetworks.Instance.Regtest);
            this.receiver = BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest);
            this.property = new PropertyId(3);
            this.amount = new PropertyAmount(1);
        }

        [Fact]
        public void Constructor_WithNullSender_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "sender",
                () => new SimpleSendV0(null, this.receiver, this.property, this.amount)
            );
        }

        [Fact]
        public void Constructor_WithNullProperty_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "property",
                () => new SimpleSendV0(this.sender, this.receiver, null, this.amount)
            );
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(0L)]
        public void Constructor_WithInvalidAmount_ShouldThrow(long value)
        {
            var amount = new PropertyAmount(value);

            Assert.Throws<ArgumentOutOfRangeException>(
                "amount",
                () => new SimpleSendV0(this.sender, this.receiver, this.property, amount)
            );
        }

        [Fact]
        public void Constructor_WithValidArguments_ShouldSuccess()
        {
            var tx = new SimpleSendV0(this.sender, this.receiver, this.property, this.amount);

            Assert.Equal(SimpleSendV0.StaticId, tx.Id);
            Assert.Equal(0, tx.Version);
            Assert.Equal(this.sender, tx.Sender);
            Assert.Equal(this.receiver, tx.Receiver);
            Assert.Equal(this.property, tx.Property);
            Assert.Equal(this.amount, tx.Amount);
        }
    }
}
