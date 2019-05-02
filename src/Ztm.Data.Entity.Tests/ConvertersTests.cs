using System;
using NBitcoin;
using Xunit;

namespace Ztm.Data.Entity.Tests
{
    public class ConvertersTests
    {
        [Fact]
        public void UInt256ToBytesConverter_ToProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (byte[])Converters.UInt256ToBytesConverter.ConvertToProvider(uint256.One);

            Assert.Equal(uint256.One.ToBytes(), converted);
        }

        [Fact]
        public void UInt256ToBytesConverter_FromProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (uint256)Converters.UInt256ToBytesConverter.ConvertFromProvider(uint256.One.ToBytes());

            Assert.Equal(uint256.One, converted);
        }

        [Fact]
        public void UriToStringConverter_ToProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (string)Converters.UriToStringConverter.ConvertToProvider(new Uri("http://127.0.0.1:8888/"));

            Assert.Equal("http://127.0.0.1:8888/", converted);
        }

        [Fact]
        public void UriToStringConverter_FromProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (Uri)Converters.UriToStringConverter.ConvertFromProvider("http://127.0.0.1:8888/");

            Assert.Equal(new Uri("http://127.0.0.1:8888/"), converted);
        }
    }
}
