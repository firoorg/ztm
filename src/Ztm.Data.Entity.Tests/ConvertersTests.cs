using System;
using System.Net;
using NBitcoin;
using Xunit;

namespace Ztm.Data.Entity.Tests
{
    public class ConvertersTests
    {
        [Fact]
        public void IPAddressToStringConverter_ToProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (string)Converters.IPAddressToStringConverter.ConvertToProvider(IPAddress.IPv6Loopback);

            Assert.Equal(IPAddress.IPv6Loopback.ToString(), converted);
        }

        [Fact]
        public void IPAddressToStringConverter_FromProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (IPAddress)Converters.IPAddressToStringConverter.ConvertFromProvider("127.0.0.1");

            Assert.Equal(IPAddress.Loopback, converted);
        }

        [Fact]
        public void ScriptToBytesConverter_ToProviderWithCorrectInput_ShouldSuccess()
        {
            var value = new Script("OP_DUP OP_HASH160 dfb52dff01bf04f983d6255e2ab9ff4084dd7517 OP_EQUALVERIFY OP_CHECKSIG");
            var converted = (byte[])Converters.ScriptToBytesConverter.ConvertToProvider(value);

            Assert.Equal(value.ToBytes(), converted);
        }

        [Fact]
        public void ScriptToBytesConverter_FromProviderWithCorrectInput_ShouldSuccess()
        {
            var value = new Script("OP_DUP OP_HASH160 dfb52dff01bf04f983d6255e2ab9ff4084dd7517 OP_EQUALVERIFY OP_CHECKSIG");
            var converted = (Script)Converters.ScriptToBytesConverter.ConvertFromProvider(value.ToBytes());

            Assert.Equal(value, converted);
        }

        [Fact]
        public void TargetToInt64_ToProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (long)Converters.TargetToInt64.ConvertToProvider(Target.Difficulty1);

            Assert.Equal(Target.Difficulty1.ToCompact(), converted);
        }

        [Fact]
        public void TargetToInt64_FromProviderWithCorrectInput_ShouldSuccess()
        {
            var converted = (Target)Converters.TargetToInt64.ConvertFromProvider((long)Target.Difficulty1.ToCompact());

            Assert.Equal(Target.Difficulty1, converted);
        }

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
