using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.WebApi.Binders;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.WebApi.Tests.Binders
{
    public sealed class BitcoinAddressModelBinderProviderTests :
        ProviderTests<BitcoinAddressModelBinderProvider, BitcoinAddress>
    {
        public BitcoinAddressModelBinderProviderTests()
        {
            Services.GetService(typeof(Network)).Returns(ZcoinNetworks.Instance.Regtest);
        }

        [Fact]
        public void GetBinder_WithNullContext_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("context", () => Subject.GetBinder(null));
        }

        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(short))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        [InlineData(typeof(string))]
        public void GetBinder_WithUnsupportType_ShouldReturnNull(Type type)
        {
            // Arrange.
            var meta = Substitute.ForPartsOf<ModelMetadata>(ModelMetadataIdentity.ForType(type));

            Context.Metadata.Returns(meta);

            // Act.
            var binder = Subject.GetBinder(Context);

            // Assert.
            Assert.Null(binder);
        }

        [Fact]
        public void GetBinder_WithSupportType_ShouldSuccess()
        {
            var binder = Subject.GetBinder(Context);

            Assert.IsType<BitcoinAddressModelBinder>(binder);
        }
    }
}
