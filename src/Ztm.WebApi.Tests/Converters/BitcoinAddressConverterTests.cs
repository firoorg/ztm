using System;
using Moq;
using NBitcoin;
using Newtonsoft.Json;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Converters;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.WebApi.Tests.Converters
{
    public sealed class BitcoinAddressConverterTests : ConverterTesting<BitcoinAddressConverter, BitcoinAddress>
    {
        readonly Network network;

        public BitcoinAddressConverterTests()
        {
            var address = "TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA";

            this.network = ZcoinNetworks.Instance.Regtest;

            ValidValue = Tuple.Create(address, BitcoinAddress.Create(address, this.network));
        }

        protected override string InvalidValue => "a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM"; // mainnet address should fail

        protected override Tuple<string, BitcoinAddress> ValidValue { get; }

        protected override BitcoinAddressConverter CreateSubject()
        {
            return new BitcoinAddressConverter(this.network);
        }

        [Fact]
        public void Constructor_WithNullNetwork_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("network", () => new BitcoinAddressConverter(null));
        }

        [Fact]
        public void ReadJson_WithNullToken_ShouldReturnNull()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.Null);

            // Act.
            var result = Subject.ReadJson(JsonReader.Object, typeof(BitcoinAddress), null, false, JsonSerializer);

            // Assert.
            Assert.Null(result);
        }

        [Fact]
        public void ReadJson_WithValidString_ShouldReturnParsedValue()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.String);
            JsonReader.SetupGet(r => r.Value).Returns("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA");

            // Act.
            var result = Subject.ReadJson(JsonReader.Object, typeof(BitcoinAddress), null, false, JsonSerializer);

            // Assert.
            Assert.Equal("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", result.ToString());
        }

        [Theory]
        [EnumMemberData(typeof(JsonToken), JsonToken.Null, JsonToken.String)]
        public void ReadJson_WithUnsupportedType_ShouldThrow(JsonToken token)
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(token);

            // Act.
            Assert.Throws<JsonSerializationException>(
                () => Subject.ReadJson(JsonReader.Object, typeof(BitcoinAddress), null, false, JsonSerializer));
        }

        [Fact]
        public void WriteJson_WithNullValue_ShouldWriteNull()
        {
            // Act.
            Subject.WriteJson(JsonWriter.Object, null, JsonSerializer);

            // Assert.
            JsonWriter.Verify(w => w.WriteNull(), Times.Once());
            JsonWriter.VerifyNoOtherCalls();
        }

        [Fact]
        public void WriteJson_WithNonNullValue_ShouldWriteString()
        {
            // Act.
            Subject.WriteJson(JsonWriter.Object, TestAddress.Regtest1, JsonSerializer);

            // Assert.
            JsonWriter.Verify(w => w.WriteValue(TestAddress.Regtest1.ToString()), Times.Once());
            JsonWriter.VerifyNoOtherCalls();
        }
    }
}
