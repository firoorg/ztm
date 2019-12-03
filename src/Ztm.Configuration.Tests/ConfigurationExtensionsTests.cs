using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Xunit;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Configuration.Tests
{
    public sealed class ConfigurationExtensionsTests
    {
        readonly IConfiguration config;

        public ConfigurationExtensionsTests()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Database:Main:ConnectionString", "Host=127.0.0.1;Database=ztm;Username=ztm;Password=1234"},
                {"Zcoin:Network:Type", "Testnet"},
                {"Zcoin:Rpc:Address", "http://127.0.0.1:8888"},
                {"Zcoin:Rpc:UserName", "root"},
                {"Zcoin:Rpc:Password", "abc"},
                {"Zcoin:Property:Id", "1"},
                {"Zcoin:Property:Type", "Divisible"},
                {"Zcoin:Property:Issuer", "Mainnet:a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM"},
                {"Zcoin:Property:Distributor", "Testnet:TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA"},
                {"Zcoin:ZeroMq:Address", "tcp://127.0.0.1:5555"}
            });

            this.config = builder.Build();
        }

        [Fact]
        public void GetDatabaseSection_WithCorrectConfiguration_ShouldSuccess()
        {
            var parsed = this.config.GetDatabaseSection();

            Assert.NotNull(parsed);
            Assert.NotNull(parsed.Main);
            Assert.Equal("Host=127.0.0.1;Database=ztm;Username=ztm;Password=1234", parsed.Main.ConnectionString);
        }

        [Fact]
        public void GetZcoinSection_WithCorrectConfiguration_ShouldSuccess()
        {
            var parsed = this.config.GetZcoinSection();

            Assert.Equal(NetworkType.Testnet, parsed.Network.Type);
            Assert.Equal(new Uri("http://127.0.0.1:8888"), parsed.Rpc.Address);
            Assert.Equal("root", parsed.Rpc.UserName);
            Assert.Equal("abc", parsed.Rpc.Password);
            Assert.Equal(new PropertyId(1), parsed.Property.Id);
            Assert.Equal(PropertyType.Divisible, parsed.Property.Type);
            Assert.Equal(NetworkType.Mainnet, parsed.Property.Issuer.Type);
            Assert.Equal(BitcoinAddress.Create("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM", ZcoinNetworks.Instance.Mainnet), parsed.Property.Issuer.Address);
            Assert.Equal(NetworkType.Testnet, parsed.Property.Distributor.Type);
            Assert.Equal(BitcoinAddress.Create("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", ZcoinNetworks.Instance.Testnet), parsed.Property.Distributor.Address);
            Assert.Equal("tcp://127.0.0.1:5555", parsed.ZeroMq.Address);
        }
    }
}
