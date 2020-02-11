using System;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Ztm.Testing;
using Ztm.Zcoin.NBitcoin.Exodus;
using PropertyAmountConverter=Ztm.WebApi.Converters.PropertyAmountConverter;

namespace Ztm.WebApi.Tests.Converters
{
    public abstract class PropertyAmountConverterTests :
        ConverterTesting<PropertyAmountConverter, PropertyAmount, PropertyAmount?>
    {
        protected PropertyAmountConverterTests()
        {
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(string))]
        public void ReadJson_ToUnsupportedType_ShouldThrow(Type destination)
        {
            Assert.Throws<ArgumentException>(
                "objectType",
                () => Subject.ReadJson(JsonReader.Object, destination, null, JsonSerializer));
        }

        [Fact]
        public void ReadJson_WithNullTokenOnNonNullable_ShouldThrow()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.Null);

            // Act.
            Assert.Throws<JsonSerializationException>(
                () => Subject.ReadJson(JsonReader.Object, typeof(PropertyAmount), null, JsonSerializer));
        }

        [Fact]
        public void ReadJson_WithNullTokenOnNullable_ShouldReturnNull()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.Null);

            // Act.
            var result = Subject.ReadJson(JsonReader.Object, typeof(PropertyAmount?), null, JsonSerializer);

            // Assert.
            Assert.Null(result);
        }

        [Theory]
        [EnumMemberData(typeof(JsonToken), JsonToken.Null, JsonToken.String)]
        public void ReadJson_WithUnsupportedType_ShouldThrow(JsonToken token)
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(token);

            // Act.
            Assert.Throws<JsonSerializationException>(
                () => Subject.ReadJson(
                    JsonReader.Object,
                    typeof(PropertyAmount),
                    default(PropertyAmount),
                    JsonSerializer));
        }
    }

    public sealed class DivisiblePropertyAmountConverterTests : PropertyAmountConverterTests
    {
        protected override string InvalidValue => "abc";

        protected override Tuple<string, PropertyAmount> ValidValue => Tuple.Create(
            "1",
            PropertyAmount.FromDivisible(1));

        protected override PropertyAmountConverter CreateSubject()
        {
            return new PropertyAmountConverter(PropertyType.Divisible);
        }

        [Fact]
        public void ReadJson_WithValidString_ShouldReturnParsedValue()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.String);
            JsonReader.SetupGet(r => r.Value).Returns("1");

            // Act.
            var result = Subject.ReadJson(
                JsonReader.Object,
                typeof(PropertyAmount),
                default(PropertyAmount),
                JsonSerializer);

            // Assert.
            Assert.Equal(PropertyAmount.FromDivisible(1), result);
        }

        [Fact]
        public void WriteJson_WithValidArgs_ShouldWriteString()
        {
            // Arrange.
            var value = PropertyAmount.FromDivisible(1);

            // Act.
            Subject.WriteJson(JsonWriter.Object, value, JsonSerializer);

            // Assert.
            JsonWriter.Verify(w => w.WriteValue("1.00000000"), Times.Once());
            JsonWriter.VerifyNoOtherCalls();
        }
    }

    public sealed class IndivisiblePropertyAmountConverterTests : PropertyAmountConverterTests
    {
        protected override string InvalidValue => "1.0";

        protected override Tuple<string, PropertyAmount> ValidValue => Tuple.Create("1", new PropertyAmount(1));

        protected override PropertyAmountConverter CreateSubject()
        {
            return new PropertyAmountConverter(PropertyType.Indivisible);
        }

        [Fact]
        public void ReadJson_WithValidString_ShouldReturnParsedValue()
        {
            // Arrange.
            JsonReader.SetupGet(r => r.TokenType).Returns(JsonToken.String);
            JsonReader.SetupGet(r => r.Value).Returns("1");

            // Act.
            var result = Subject.ReadJson(
                JsonReader.Object,
                typeof(PropertyAmount),
                default(PropertyAmount),
                JsonSerializer);

            // Assert.
            Assert.Equal(new PropertyAmount(1), result);
        }

        [Fact]
        public void WriteJson_WithValidArgs_ShouldWriteString()
        {
            // Arrange.
            var value = new PropertyAmount(1);

            // Act.
            Subject.WriteJson(JsonWriter.Object, value, JsonSerializer);

            // Assert.
            JsonWriter.Verify(w => w.WriteValue("1"), Times.Once());
            JsonWriter.VerifyNoOtherCalls();
        }
    }
}
