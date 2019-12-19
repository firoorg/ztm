using System;
using System.IO;
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
