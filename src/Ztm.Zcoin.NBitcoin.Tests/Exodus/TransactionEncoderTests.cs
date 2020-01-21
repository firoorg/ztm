using System;
using System.IO;
using System.Net;
using System.Text;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class TransactionEncoderTests
    {
        readonly ITransactionPayloadEncoder encoder0, encoder1;
        readonly TransactionEncoder subject;

        public TransactionEncoderTests()
        {
            this.encoder0 = Substitute.For<ITransactionPayloadEncoder>();
            this.encoder0.Type.Returns(0);

            this.encoder1 = Substitute.For<ITransactionPayloadEncoder>();
            this.encoder1.Type.Returns(1);

            this.subject = new TransactionEncoder(new[] { this.encoder0, this.encoder1 });
        }

        [Fact]
        public void Encode_WithNullTransaction_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "transaction",
                () => this.subject.Encode(null)
            );
        }

        [Theory]
        [InlineData(2)]
        [InlineData(Int32.MaxValue)]
        public void Encode_WithUnsupportedTransaction_ShouldThrow(int id)
        {
            var tx = new FakeExodusTransaction(null, null, id, 0);

            Assert.Throws<ArgumentException>(
                "transaction",
                () => this.subject.Encode(tx)
            );
        }

        [Theory]
        [InlineData(1, 1, (byte)0x00)]
        [InlineData(1, 1, (byte)0x00, (byte)0x00, (byte)0x01)]
        [InlineData(1, 0, (byte)0x00)]
        [InlineData(short.MinValue, 1, (byte)0x00)]
        [InlineData(0, 1, (byte)0x00)]
        [InlineData(short.MaxValue, 1, (byte)0x00)]
        public void Encode_WithValidData_ShouldSuccess(int version, int id, params byte[] payload)
        {
            // Arrange.
            ExodusTransaction tx = new FakeExodusTransaction(null, null, id, version);

            byte[] expected;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(IPAddress.HostToNetworkOrder((short)version));
                writer.Write(IPAddress.HostToNetworkOrder((short)id));
                writer.Write(payload);

                expected = stream.ToArray();
            }

            ITransactionPayloadEncoder encoder;
            switch (id)
            {
                case 0:
                    encoder = this.encoder0;
                    break;
                case 1:
                    encoder = this.encoder1;
                    break;
                default:
                    throw new InvalidOperationException("Type is not supported.");
            }

            encoder.When(e => e.Encode(Arg.Any<BinaryWriter>(), tx))
                   .Do(info =>
                   {
                       var writer = info.ArgAt<BinaryWriter>(0);
                       writer.Write(payload);
                   });

            // Act.
            var result = this.subject.Encode(tx);

            // Assert.
            encoder.Received(1).Encode(Arg.Any<BinaryWriter>(), tx);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decode_WithNullData_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "data",
                () => this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, null)
            );
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
                () => this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, data)
            );

            Assert.Equal(4, ex.RequiredSize);
        }

        [Fact]
        public void Decode_WithInvalidTransactionType_ShouldThrow()
        {
            byte[] data;

            using (var stream = RawTransaction.Create(2, 0))
            {
                data = stream.ToArray();
            }

            var ex = Assert.Throws<TransactionFieldException>(
                () => this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, data)
            );

            Assert.Equal(TransactionFieldException.TypeField, ex.Field);
        }

        [Fact]
        public void Decode_WithValidTransactionType_ShouldInvokePayloadDecode()
        {
            // Arrange.
            ExodusTransaction tx = new FakeExodusTransaction(null, null);
            byte[] data;

            using (var stream = RawTransaction.Create(1, ExodusTransaction.MaxVersion))
            {
                data = stream.ToArray();
            }

            this.encoder1.Decode(
                Arg.Any<BitcoinAddress>(),
                Arg.Any<BitcoinAddress>(),
                Arg.Any<BinaryReader>(),
                Arg.Any<int>()
            ).Returns(tx);

            // Act.
            var result = this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, data);

            // Assert.
            Assert.Same(tx, result);

            this.encoder0.Received(0).Decode(
                Arg.Any<BitcoinAddress>(),
                Arg.Any<BitcoinAddress>(),
                Arg.Any<BinaryReader>(),
                Arg.Any<int>()
            );

            this.encoder1.Received(1).Decode(
                TestAddress.Regtest1,
                TestAddress.Regtest2,
                Arg.Is<BinaryReader>(r => r != null),
                ExodusTransaction.MaxVersion
            );
        }
    }
}
