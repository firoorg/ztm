using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
using Ztm.WebApi.Binders;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Tests.Binders
{
    public sealed class PropertyAmountModelBinderTests
    {
        readonly IConfiguration divisibleConfig, indivisibleConfig;
        readonly IValueProvider values;
        readonly ModelMetadata meta;
        readonly ModelBindingContext context;

        public PropertyAmountModelBinderTests()
        {
            this.divisibleConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Zcoin:Property:Type", "Divisible"}
            }).Build();

            this.indivisibleConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Zcoin:Property:Type", "Indivisible"}
            }).Build();

            this.values = Substitute.For<IValueProvider>();
            this.meta = Substitute.ForPartsOf<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(PropertyAmount)));
            this.context = Substitute.ForPartsOf<ModelBindingContext>();
            this.context.ModelMetadata.Returns(this.meta);
            this.context.ModelState.Returns(new ModelStateDictionary());
            this.context.ValueProvider.Returns(this.values);
        }

        [Fact]
        public async Task BindModelAsync_WithNullBindingContext_ShouldThrow()
        {
            var subject = new PropertyAmountModelBinder(this.divisibleConfig);

            await Assert.ThrowsAsync<ArgumentNullException>(
                "bindingContext",
                () => subject.BindModelAsync(null)
            );
        }

        [Fact]
        public async Task BindModelAsync_NoSubmittedValue_ShouldFail()
        {
            // Arrange.
            var subject = new PropertyAmountModelBinder(this.divisibleConfig);

            this.context.ModelName.Returns("Amount");
            this.values.GetValue("Amount").Returns(ValueProviderResult.None);

            // Act.
            await subject.BindModelAsync(context);

            // Assert.
            Assert.False(context.Result.IsModelSet);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task BindModelAsync_WithEmptySubmittedValue_ShouldFail(string value)
        {
            // Arrange.
            var subject = new PropertyAmountModelBinder(this.divisibleConfig);

            this.context.ModelName.Returns("Amount");
            this.values.GetValue("Amount").Returns(new ValueProviderResult(new[] { value }));

            // Act.
            await subject.BindModelAsync(context);

            // Assert.
            Assert.False(context.Result.IsModelSet);
            Assert.Equal(value, this.context.ModelState["Amount"].RawValue);
        }

        [Theory]
        [InlineData("0.00000001", "0.00000001")]
        [InlineData("9.9", "9.90000000")]
        [InlineData("10000", "10000.00000000")]
        [InlineData("92233720368.54775807", "92233720368.54775807")]
        public async Task BindModelAsync_WithValidDivisibleValue_ShouldSuccess(string value, string expected)
        {
            // Arrange.
            var subject = new PropertyAmountModelBinder(this.divisibleConfig);

            this.context.ModelName.Returns("Amount");
            this.values.GetValue("Amount").Returns(new ValueProviderResult(new[] { value }));

            // Act.
            await subject.BindModelAsync(context);

            // Assert.
            var model = (PropertyAmount)context.Result.Model;

            Assert.True(context.Result.IsModelSet);
            Assert.Equal(expected, model.ToString(PropertyType.Divisible));
            Assert.Equal(value, this.context.ModelState["Amount"].RawValue);
        }

        [Theory]
        [InlineData("0.100000001")]
        [InlineData("92233720368.54775808")]
        public async Task BindModelAsync_WithInvalidDivisibleValue_ShouldFail(string value)
        {
            // Arrange.
            var subject = new PropertyAmountModelBinder(this.divisibleConfig);

            this.context.ModelName.Returns("Amount");
            this.values.GetValue("Amount").Returns(new ValueProviderResult(new[] { value }));

            // Act.
            await subject.BindModelAsync(context);

            // Assert.
            Assert.False(context.Result.IsModelSet);
            Assert.NotEmpty(this.context.ModelState["Amount"].Errors);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("10000")]
        [InlineData("9223372036854775807")]
        public async Task BindModelAsync_WithValidIndivisibleValue_ShouldSuccess(string value)
        {
            // Arrange.
            var subject = new PropertyAmountModelBinder(this.indivisibleConfig);

            this.context.ModelName.Returns("Amount");
            this.values.GetValue("Amount").Returns(new ValueProviderResult(new[] { value }));

            // Act.
            await subject.BindModelAsync(context);

            // Assert.
            var model = (PropertyAmount)context.Result.Model;

            Assert.True(context.Result.IsModelSet);
            Assert.Equal(value, model.ToString(PropertyType.Indivisible));
            Assert.Equal(value, this.context.ModelState["Amount"].RawValue);
        }

        [Theory]
        [InlineData("-9223372036854775809")]
        [InlineData("9223372036854775808")]
        public async Task BindModelAsync_WithInvalidIndivisibleValue_ShouldFail(string value)
        {
            // Arrange.
            var subject = new PropertyAmountModelBinder(this.indivisibleConfig);

            this.context.ModelName.Returns("Amount");
            this.values.GetValue("Amount").Returns(new ValueProviderResult(new[] { value }));

            // Act.
            await subject.BindModelAsync(context);

            // Assert.
            Assert.False(context.Result.IsModelSet);
            Assert.NotEmpty(this.context.ModelState["Amount"].Errors);
        }
    }
}
