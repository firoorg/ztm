using System;
using System.IO;
using System.Net;
using System.Text;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class TransactionDecoderTests
    {
        readonly BitcoinAddress sender;
        readonly BitcoinAddress receiver;
        readonly TransactionDecoder subject;

        public TransactionDecoderTests()
        {
            this.sender = BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", ZcoinNetworks.Instance.Regtest);
            this.receiver = BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", ZcoinNetworks.Instance.Regtest);
            this.subject = new TransactionDecoder();
        }

        [Fact]
        public void Decode_WithNullData_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("data", () => this.subject.Decode(this.sender, this.receiver, null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Decode_WithTooShortData_ShouldThrow(int length)
        {
            var data = new byte[length];
            var ex = Assert.Throws<TransactionTooShortException>(
                () => this.subject.Decode(this.sender, this.receiver, data)
            );

            Assert.Equal(4, ex.RequiredSize);
        }

        [Theory]
        [InlineData(65532)]
        public void Decode_WithInvalidTransactionType_ShouldThrow(int type)
        {
            byte[] data;

            using (var stream = MockData(type, 0))
            {
                data = stream.ToArray();
            }

            var ex = Assert.Throws<TransactionFieldException>(
                () => this.subject.Decode(this.sender, this.receiver, data)
            );

            Assert.Equal(TransactionFieldException.TypeField, ex.Field);
        }

        [Fact]
        public void Decode_SimpleSendWithInvalidVersion_ShouldThrow()
        {
            byte[] data;

            using (var stream = MockData(SimpleSendV0.StaticId, 1))
            {
                data = stream.ToArray();
            }

            var ex = Assert.Throws<TransactionFieldException>(
                () => this.subject.Decode(this.sender, this.receiver, data)
            );

            Assert.Equal(TransactionFieldException.VersionField, ex.Field);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        public void Decode_SimpleSendV0WithTooShortData_ShouldThrow(int length)
        {
            var payload = new byte[length - 4]; // Just a payload for simple send, not including version and type.
            byte[] data;

            using (var stream = new MemoryStream(payload))
            {
                if (payload.Length >= 4)
                {
                    WritePropertyId(stream, new PropertyId(1));
                }
            }

            using (var stream = MockData(SimpleSendV0.StaticId, 0))
            {
                stream.Write(payload, 0, payload.Length);

                data = stream.ToArray();
            }

            var ex = Assert.Throws<TransactionTooShortException>(
                () => this.subject.Decode(this.sender, this.receiver, data)
            );

            Assert.Equal(16, ex.RequiredSize);
        }

        [Fact]
        public void Decode_SimpleSendV0WithInvalidProperty_ShouldThrow()
        {
            byte[] data;

            using (var stream = MockData(SimpleSendV0.StaticId, 0))
            {
                WritePropertyId(stream, 0);
                WritePropertyAmount(stream, PropertyAmount.One);

                data = stream.ToArray();
            }

            var ex = Assert.Throws<TransactionFieldException>(
                () => this.subject.Decode(this.sender, this.receiver, data)
            );

            Assert.Equal("property", ex.Field);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        [InlineData(long.MinValue)]
        public void Decode_SimpleSendV0WithInvalidAmount_ShouldThrow(long amount)
        {
            byte[] data;

            using (var stream = MockData(SimpleSendV0.StaticId, 0))
            {
                WritePropertyId(stream, new PropertyId(PropertyId.MaxValue));
                WritePropertyAmount(stream, new PropertyAmount(amount));

                data = stream.ToArray();
            }

            var ex = Assert.Throws<TransactionFieldException>(
                () => this.subject.Decode(this.sender, this.receiver, data)
            );

            Assert.Equal("amount", ex.Field);
        }

        [Theory]
        [InlineData(PropertyId.MinValue, 1L)]
        [InlineData(PropertyId.MinValue, long.MaxValue)]
        [InlineData(PropertyId.MaxValue, 1L)]
        [InlineData(PropertyId.MaxValue, long.MaxValue)]
        public void Decode_SimpleSendV0WithValidData_ShouldSuccess(long property, long amount)
        {
            byte[] data;

            using (var stream = MockData(SimpleSendV0.StaticId, 0))
            {
                WritePropertyId(stream, new PropertyId(property));
                WritePropertyAmount(stream, new PropertyAmount(amount));

                data = stream.ToArray();
            }

            var tx = (SimpleSendV0)this.subject.Decode(this.sender, this.receiver, data);

            Assert.IsType<SimpleSendV0>(tx); // We want to be the exact V0.
            Assert.Equal(SimpleSendV0.StaticId, tx.Id);
            Assert.Equal(0, tx.Version);
            Assert.Same(this.sender, tx.Sender);
            Assert.Same(this.receiver, tx.Receiver);
            Assert.Equal(property, tx.Property.Value);
            Assert.Equal(amount, tx.Amount.Indivisible);
        }

        MemoryStream MockData(int type, int version)
        {
            var data = new MemoryStream();

            try
            {
                using (var writer = new BinaryWriter(data, Encoding.UTF8, true))
                {
                    writer.Write(IPAddress.HostToNetworkOrder((short)version));
                    writer.Write(IPAddress.HostToNetworkOrder((short)type));
                }
            }
            catch
            {
                data.Dispose();
                throw;
            }

            return data;
        }

        void WritePropertyId(Stream output, PropertyId id)
        {
            using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder((int)id.Value));
            }
        }

        void WritePropertyId(Stream output, int id)
        {
            using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder(id));
            }
        }

        void WritePropertyAmount(Stream output, PropertyAmount amount)
        {
            using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder(amount.Indivisible));
            }
        }
    }
}
