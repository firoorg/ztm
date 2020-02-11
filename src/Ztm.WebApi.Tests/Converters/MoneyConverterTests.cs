using System;
using Moq;
using NBitcoin;
using Newtonsoft.Json;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Converters;

namespace Ztm.WebApi.Tests.Converters
{
    public sealed class MoneyConverterTests : ConverterTesting<MoneyConverter, Money>
    {
        protected override string InvalidValue => "abc";

        protected override Tuple<string, Money> ValidValue => Tuple.Create("1", Money.Coins(1));

        protected override MoneyConverter CreateSubject()
        {
            return new MoneyConverter();
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(DerivedMoney))]
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
            var result = Subject.ReadJson(JsonReader.Object, typeof(Money), null, JsonSerializer);

            // Assert.
            Assert.Null(result);
        }

        [Fact]
        public void ReadJson_WithValidString_ShouldReturnParsedData()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.String);
            JsonReader.SetupGet(r => r.Value).Returns("1");

            // Act.
            var result = Subject.ReadJson(JsonReader.Object, typeof(Money), null, JsonSerializer);

            // Assert.
            Assert.Equal(Money.Coins(1), result);
        }

        [Theory]
        [EnumMemberData(typeof(JsonToken), JsonToken.Null, JsonToken.String)]
        public void ReadJson_WithUnsupportedValue_ShouldThrow(JsonToken token)
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(token);

            // Act.
            Assert.Throws<JsonSerializationException>(
                () => Subject.ReadJson(JsonReader.Object, typeof(Money), null, JsonSerializer));
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
            // Arrange.
            var value = Money.Coins(1);

            // Act.
            Subject.WriteJson(JsonWriter.Object, value, JsonSerializer);

            // Assert.
            JsonWriter.Verify(w => w.WriteValue("1.00000000"), Times.Once());
            JsonWriter.VerifyNoOtherCalls();
        }

        public sealed class DerivedMoney : Money
        {
            public DerivedMoney(long satoshis)
                : base(satoshis)
            {
            }
        }
    }
}
