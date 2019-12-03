using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NBitcoin;
using NSubstitute;
using Xunit;
using Ztm.WebApi.Binders;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.WebApi.Tests.Binders
{
    public sealed class BitcoinAddressModelBinderTests : BinderTests<BitcoinAddress>
    {
        readonly Network network;
        readonly BitcoinAddressModelBinder subject;

        public BitcoinAddressModelBinderTests()
        {
            this.network = ZcoinNetworks.Instance.Regtest;
            this.subject = new BitcoinAddressModelBinder(this.network);
        }

        [Fact]
        public void Constructor_WithNullNetwork_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("network", () => new BitcoinAddressModelBinder(null));
        }

        [Fact]
        public async Task BindModelAsync_WithNullContext_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>("bindingContext", () => this.subject.BindModelAsync(null));
        }

        [Fact]
        public async Task BindModelAsync_WithoutAnyValues_ShouldNotAssignResult()
        {
            // Act.
            await this.subject.BindModelAsync(Context);

            // Assert.
            Assert.Equal(ModelBindingResult.Failed(), Context.Result);
            Assert.Empty(Context.ModelState);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task BindModelAsync_WithEmptyValue_ShouldAssignModelValue(string value)
        {
            // Arrange.
            Context.ModelName = "address";
            ValueProvider.GetValue("address").Returns(new ValueProviderResult(new[] { value }));

            // Act.
            await this.subject.BindModelAsync(Context);

            // Assert.
            Assert.Equal(ModelBindingResult.Failed(), Context.Result);
            Assert.Single(Context.ModelState);
            Assert.Equal(value, Context.ModelState["address"].RawValue);
            Assert.Empty(Context.ModelState["address"].Errors);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM")] // mainnet address should fail
        public async Task BindModelAsync_WithInvalidValue_ShouldAssignModelError(string value)
        {
            // Arrange.
            Context.ModelName = "address";
            ValueProvider.GetValue("address").Returns(new ValueProviderResult(new[] { value }));

            // Act.
            await this.subject.BindModelAsync(Context);

            // Assert.
            Assert.Equal(ModelBindingResult.Failed(), Context.Result);
            Assert.Single(Context.ModelState);
            Assert.Equal(value, Context.ModelState["address"].RawValue);
            Assert.Single(Context.ModelState["address"].Errors);
        }

        [Fact]
        public async Task BindModelAsync_WithValidValue_ShouldAssignResult()
        {
            // Arrange.
            Context.ModelName = "address";
            ValueProvider.GetValue("address").Returns(new ValueProviderResult("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA"));

            // Act.
            await this.subject.BindModelAsync(Context);

            // Assert.
            Assert.Single(Context.ModelState);
            Assert.Equal("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", Context.ModelState["address"].RawValue);
            Assert.Empty(Context.ModelState["address"].Errors);
            Assert.IsAssignableFrom<BitcoinAddress>(Context.Result.Model);
            Assert.Equal("TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA", Context.Result.Model.ToString());
        }
    }
}
