using System;
using System.IO;
using System.Net;
using System.Text;
using NSubstitute;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    public sealed class TransactionPayloadEncoderTests : IDisposable
    {
        readonly BinaryReader payload;
        readonly TestTransactionPayloadEncoder subject;

        public TransactionPayloadEncoderTests()
        {
            var stream = new MemoryStream();

            try
            {
                this.payload = new BinaryReader(stream, Encoding.UTF8);

                try
                {
                    this.subject = new TestTransactionPayloadEncoder();
                }
                catch
                {
                    this.payload.Dispose();
                    throw;
                }
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            this.payload.Dispose();
        }

        [Fact]
        public void Decode_WithNullPayload_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "payload",
                () => this.subject.InvokeDecode(
                    TestAddress.Regtest1,
                    TestAddress.Regtest2,
                    null,
                    ExodusTransaction.MinVersion
                )
            );
        }

        [Fact]
        public void Decode_WithNullSender_ShouldSuccess()
        {
            // Act.
            this.subject.InvokeDecode(null, TestAddress.Regtest1, this.payload, ExodusTransaction.MaxVersion);

            // Assert.
            this.subject.FakeDecode.Received(1)(null, TestAddress.Regtest1, this.payload, ExodusTransaction.MaxVersion);
        }

        [Fact]
        public void Decode_WithNullReceiver_ShouldSuccess()
        {
            // Act.
            this.subject.InvokeDecode(TestAddress.Regtest1, null, this.payload, ExodusTransaction.MinVersion);

            // Assert.
            this.subject.FakeDecode.Received(1)(TestAddress.Regtest1, null, this.payload, ExodusTransaction.MinVersion);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void Decode_WithInvalidVersion_ShouldSuccess(int version)
        {
            // Act.
            this.subject.InvokeDecode(TestAddress.Regtest1, TestAddress.Regtest2, this.payload, version);

            // Assert.
            this.subject.FakeDecode.Received(1)(TestAddress.Regtest1, TestAddress.Regtest2, this.payload, version);
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
        public void DecodePropertyAmount_NotEnoughData_ShouldThrow(int length)
        {
            var data = new byte[length];

            using (var stream = new MemoryStream(data, false))
            using (var reader = new BinaryReader(stream))
            {
                Assert.Throws<EndOfStreamException>(() => TestTransactionPayloadEncoder.DecodePropertyAmount(reader));
            }
        }

        [Theory]
        [InlineData(8, long.MinValue)]
        [InlineData(9, long.MaxValue)]
        [InlineData(16, 0L)]
        public void DecodePropertyAmount_WithEnoughData_ShouldSuccess(int length, long amount)
        {
            // Arrange.
            var data = new byte[length];

            using (var stream = new MemoryStream(data))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder(amount));
                stream.Seek(0, SeekOrigin.Begin);

                // Act.
                var result = TestTransactionPayloadEncoder.DecodePropertyAmount(reader);

                // Assert.
                Assert.Equal(amount, result.Indivisible);
                Assert.Equal(8L, stream.Position);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void DecodePropertyId_NotEnoughData_ShouldThrow(int length)
        {
            var data = new byte[length];

            using (var stream = new MemoryStream(data, false))
            using (var reader = new BinaryReader(stream))
            {
                Assert.Throws<EndOfStreamException>(() => TestTransactionPayloadEncoder.DecodePropertyId(reader));
            }
        }

        [Theory]
        [InlineData(4, 0)]
        [InlineData(5, 0)]
        [InlineData(8, 0)]
        public void DecodePropertyId_WithInvalidData_ShouldThrow(int length, int id)
        {
            // Arrange.
            var data = new byte[length];

            using (var stream = new MemoryStream(data))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder(id));
                stream.Seek(0, SeekOrigin.Begin);

                // Act.
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => TestTransactionPayloadEncoder.DecodePropertyId(reader)
                );
            }
        }

        [Theory]
        [InlineData(4, (int)PropertyId.MinValue)]
        [InlineData(8, unchecked((int)PropertyId.MaxValue))]
        public void DecodePropertyId_WithValidData_ShouldSuccess(int length, int id)
        {
            // Arrange.
            var data = new byte[length];

            using (var stream = new MemoryStream(data))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder(id));
                stream.Seek(0, SeekOrigin.Begin);

                // Act.
                var result = TestTransactionPayloadEncoder.DecodePropertyId(reader);

                // Assert.
                Assert.Equal((uint)id, result.Value);
                Assert.Equal(4, stream.Position);
            }
        }
    }
}
