using System;
using Moq;
using NBitcoin;
using Newtonsoft.Json;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Converters;

namespace Ztm.WebApi.Tests.Converters
{
    public sealed class UInt256ConverterTests : ConverterTesting<UInt256Converter, uint256>
    {
        public UInt256ConverterTests()
        {
            var s = "2e57555cee5a1efaeed5fc326ac0cf909942a3e5690b510af65631855db78c70";
            var v = uint256.Parse(s);

            ValidValue = Tuple.Create(s, v);
        }

        protected override string InvalidValue => "qwerty";

        protected override Tuple<string, uint256> ValidValue { get; }

        protected override UInt256Converter CreateSubject()
        {
            return new UInt256Converter();
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(byte[]))]
        public void ReadJson_ToUnsupportedType_ShouldThrow(Type destination)
        {
            Assert.Throws<ArgumentException>(
                "objectType",
                () => Subject.ReadJson(JsonReader.Object, destination, null, JsonSerializer));
        }

        [Fact]
        public void ReadJson_WithNullToken_ShouldReturnNull()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.Null);

            // Act.
            var result = Subject.ReadJson(JsonReader.Object, typeof(uint256), null, JsonSerializer);

            // Assert.
            Assert.Null(result);
        }

        [Fact]
        public void ReadJson_WithValidString_ShouldReturnParsedValue()
        {
            // Arrange.
            var s = "2e57555cee5a1efaeed5fc326ac0cf909942a3e5690b510af65631855db78c70";

            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.String);
            JsonReader.SetupGet(r => r.Value).Returns(s);

            // Act.
            var result = Subject.ReadJson(JsonReader.Object, typeof(uint256), null, JsonSerializer);

            // Assert
            Assert.Equal(uint256.Parse(s), result);
        }

        [Theory]
        [EnumMemberData(typeof(JsonToken), JsonToken.Null, JsonToken.String)]
        public void ReadJson_WithUnsupportedValue_ShouldThrow(JsonToken type)
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(type);

            // Act.
            Assert.Throws<JsonSerializationException>(
                () => Subject.ReadJson(JsonReader.Object, typeof(uint256), null, JsonSerializer));
        }

        [Fact]
        public void WriteJson_WithNull_ShouldWriteNull()
        {
            // Act.
            Subject.WriteJson(JsonWriter.Object, null, JsonSerializer);

            // Assert.
            JsonWriter.Verify(w => w.WriteNull(), Times.Once());
            JsonWriter.VerifyNoOtherCalls();
        }

        [Fact]
        public void WriteJson_WithNonNull_ShouldWriteString()
        {
            // Arrange.
            var v = uint256.Parse("2e57555cee5a1efaeed5fc326ac0cf909942a3e5690b510af65631855db78c70");

            // Act.
            Subject.WriteJson(JsonWriter.Object, v, JsonSerializer);

            // Assert.
            JsonWriter.Verify(w => w.WriteValue(v.ToString()), Times.Once());
            JsonWriter.VerifyNoOtherCalls();
        }
    }
}
