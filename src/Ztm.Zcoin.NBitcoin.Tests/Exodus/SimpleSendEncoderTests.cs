using System.IO;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class SimpleSendEncoderTests
    {
        readonly ITransactionPayloadEncoder subject;

        public SimpleSendEncoderTests()
        {
            this.subject = new SimpleSendEncoder();
        }

        [Fact]
        public void Type_WhenGetValue_ShouldReturnSimpleSendId()
        {
            Assert.Equal(SimpleSendV0.StaticId, this.subject.Type);
        }

        [Fact]
        public void Decode_WithInvalidVersion_ShouldThrow()
        {
            using (var reader = new BinaryReader(Stream.Null))
            {
                var ex = Assert.Throws<TransactionFieldException>(
                    () => this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, reader, 1)
                );

                Assert.Equal(TransactionFieldException.VersionField, ex.Field);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        public void Decode_V0WithTooShortData_ShouldThrow(int length)
        {
            // Arrange.
            var payload = new byte[length];

            using (var stream = new MemoryStream(payload))
            using (var reader = new BinaryReader(stream))
            {
                if (payload.Length >= 4)
                {
                    RawTransaction.WritePropertyId(stream, PropertyId.MinValue);
                }

                stream.Seek(0, SeekOrigin.Begin);

                // Act.
                var ex = Assert.ThrowsAny<TransactionTooShortException>(
                    () => this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, reader, 0)
                );

                // Assert.
                Assert.Equal(16, ex.RequiredSize);
            }
        }

        [Fact]
        public void Decode_V0WithInvalidProperty_ShouldThrow()
        {
            // Arrange.
            using (var stream = new MemoryStream())
            using (var reader = new BinaryReader(stream))
            {
                RawTransaction.WritePropertyId(stream, 0);
                RawTransaction.WritePropertyAmount(stream, PropertyAmount.One);

                stream.Seek(0, SeekOrigin.Begin);

                // Act.
                var ex = Assert.Throws<TransactionFieldException>(
                    () => this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, reader, 0)
                );

                // Assert.
                Assert.Equal("property", ex.Field);
            }
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        [InlineData(long.MinValue)]
        public void Decode_V0WithInvalidAmount_ShouldThrow(long amount)
        {
            // Arrange.
            using (var stream = new MemoryStream())
            using (var reader = new BinaryReader(stream))
            {
                RawTransaction.WritePropertyId(stream, PropertyId.MaxValue);
                RawTransaction.WritePropertyAmount(stream, new PropertyAmount(amount));

                stream.Seek(0, SeekOrigin.Begin);

                // Act.
                var ex = Assert.Throws<TransactionFieldException>(
                    () => this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, reader, 0)
                );

                // Assert.
                Assert.Equal("amount", ex.Field);
            }
        }

        [Theory]
        [InlineData(PropertyId.MinValue, 1L)]
        [InlineData(PropertyId.MinValue, long.MaxValue)]
        [InlineData(PropertyId.MaxValue, 1L)]
        [InlineData(PropertyId.MaxValue, long.MaxValue)]
        public void Decode_V0WithValidData_ShouldSuccess(long property, long amount)
        {
            // Arrange.
            using (var stream = new MemoryStream())
            using (var reader = new BinaryReader(stream))
            {
                RawTransaction.WritePropertyId(stream, property);
                RawTransaction.WritePropertyAmount(stream, new PropertyAmount(amount));

                stream.Seek(0, SeekOrigin.Begin);

                // Act.
                var tx = (SimpleSendV0)this.subject.Decode(TestAddress.Regtest1, TestAddress.Regtest2, reader, 0);

                // Assert.
                Assert.IsType<SimpleSendV0>(tx); // We want to be the exact V0.
                Assert.Equal(this.subject.Type, tx.Id);
                Assert.Equal(0, tx.Version);
                Assert.Same(TestAddress.Regtest1, tx.Sender);
                Assert.Same(TestAddress.Regtest2, tx.Receiver);
                Assert.Equal(property, tx.Property.Value);
                Assert.Equal(amount, tx.Amount.Indivisible);
            }
        }
    }
}
