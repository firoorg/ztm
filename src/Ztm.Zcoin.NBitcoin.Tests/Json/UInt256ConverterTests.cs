using System;
using NBitcoin;
using Newtonsoft.Json;
using Xunit;
using Ztm.Zcoin.NBitcoin.Json;

namespace Ztm.Zcoin.NBitcoin.Tests.Json
{
    public sealed class UInt256ConverterTests
    {
        readonly UInt256Converter subject;

        public UInt256ConverterTests()
        {
            this.subject = new UInt256Converter();
        }

        [Theory]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000001")]
        [InlineData("b365d0d901d94c3df7f2c102db43ed5c2aa55dfb0071975c2e840f3a569133d3")]
        public void DeserializeObject_WithValidRawInt256_ShouldSuccess(string raw)
        {
            // Arrange.
            var json = JsonConvert.SerializeObject(raw);

            // Act.
            var result = JsonConvert.DeserializeObject<uint256>(json, this.subject);

            // Assert.
            Assert.Equal(uint256.Parse(raw), result);
        }

        [Theory]
        [InlineData("000000000000000000000000000000000000000000000000000000000000000g")]
        [InlineData("00000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("000000000000000000000000000000000000000000000000000000000000000")]
        public void DeserializeObject_WithInvalidRawInt256_ShouldThrow(string raw)
        {
            var json = JsonConvert.SerializeObject(raw);

            Assert.Throws<FormatException>(
                () => JsonConvert.DeserializeObject<uint256>(json, this.subject)
            );
        }

        [Theory]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000001")]
        [InlineData("b365d0d901d94c3df7f2c102db43ed5c2aa55dfb0071975c2e840f3a569133d3")]
        public void SerializeObject_WithValidInt256_ShouldSuccess(string raw)
        {
            // Arrange.
            var json = JsonConvert.SerializeObject(raw);
            var n = uint256.Parse(raw);

            // Act.
            var result = JsonConvert.SerializeObject(n, Formatting.None, this.subject);

            // Assert.
            var serialized = JsonConvert.DeserializeObject<string>(json);
            Assert.Equal(raw, serialized);
        }
    }
}