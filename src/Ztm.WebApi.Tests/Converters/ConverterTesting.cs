using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;
using Ztm.WebApi.Converters;

namespace Ztm.WebApi.Tests.Converters
{
    public abstract class ConverterTesting<TConverter, TValue> where TConverter : Converter<TValue>
    {
        readonly Lazy<TConverter> subject;

        protected ConverterTesting()
        {
            Context = new Mock<ModelBindingContext>();
            // FIXME: If we use Moq here it will not work due to Mock.CallBase does not works in the expected way.
            Metadata = Substitute.ForPartsOf<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(TValue)));
            JsonReader = new Mock<JsonReader>();
            JsonSerializer = new JsonSerializer();
            JsonWriter = new Mock<JsonWriter>();

            ModelState = new ModelStateDictionary();
            ValueProvider = new Mock<IValueProvider>();

            Context.SetupGet(c => c.ModelMetadata).Returns(Metadata);
            Context.SetupGet(c => c.ModelState).Returns(ModelState);
            Context.SetupGet(c => c.ValueProvider).Returns(ValueProvider.Object);

            this.subject = new Lazy<TConverter>(CreateSubject);
        }

        protected Mock<ModelBindingContext> Context { get; }

        protected abstract string InvalidValue { get; }

        protected Mock<JsonReader> JsonReader { get; }

        protected JsonSerializer JsonSerializer { get; }

        protected Mock<JsonWriter> JsonWriter { get; }

        protected ModelMetadata Metadata { get; }

        protected ModelStateDictionary ModelState { get; }

        protected TConverter Subject => this.subject.Value;

        protected abstract Tuple<string, TValue> ValidValue { get; }

        protected Mock<IValueProvider> ValueProvider { get; }

        protected abstract TConverter CreateSubject();

        [Fact]
        public async Task BindModelAsync_WithNullContext_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>("bindingContext", () => Subject.BindModelAsync(null));
        }

        [Fact]
        public async Task BindModelAsync_WithoutAnyValues_ShouldNotAssignResult()
        {
            // Act.
            await Subject.BindModelAsync(Context.Object);

            // Assert.
            Assert.Empty(ModelState);

            Context.VerifySet(c => c.Result = It.IsAny<ModelBindingResult>(), Times.Never());
        }

        [Fact]
        public async Task BindModelAsync_WithEmptyValue_ShouldAssignModelValue()
        {
            // Arrange.
            var name = "value";
            var value = new ValueProviderResult("");

            Context.SetupGet(c => c.ModelName).Returns(name);
            ValueProvider.Setup(p => p.GetValue(name)).Returns(value);

            // Act.
            await Subject.BindModelAsync(Context.Object);

            // Assert.
            var state = Assert.Single(ModelState);

            Assert.Equal(name, state.Key);
            Assert.Equal("", state.Value.RawValue);
            Assert.Empty(state.Value.Errors);

            Context.VerifySet(c => c.Result = It.IsAny<ModelBindingResult>(), Times.Never());
        }

        [Fact]
        public async Task BindModelAsync_WithInvalidValue_ShouldAssignModelError()
        {
            // Arrange.
            var name = "value";
            var value = new ValueProviderResult(InvalidValue);

            Context.SetupGet(c => c.ModelName).Returns(name);
            ValueProvider.Setup(p => p.GetValue(name)).Returns(value);

            // Act.
            await Subject.BindModelAsync(Context.Object);

            // Assert.
            var state = Assert.Single(ModelState);

            Assert.Equal(name, state.Key);
            Assert.Equal(InvalidValue, state.Value.RawValue);
            Assert.Single(state.Value.Errors);

            Context.VerifySet(c => c.Result = It.IsAny<ModelBindingResult>(), Times.Never());
        }

        [Fact]
        public async Task BindModelAsync_WithValidValue_ShouldAssignResult()
        {
            // Arrange.
            var name = "value";
            var value = new ValueProviderResult(ValidValue.Item1);

            Context.SetupGet(c => c.ModelName).Returns(name);
            ValueProvider.Setup(p => p.GetValue(name)).Returns(value);

            // Act.
            await Subject.BindModelAsync(Context.Object);

            // Assert.
            var state = Assert.Single(ModelState);

            Assert.Equal(name, state.Key);
            Assert.Equal(ValidValue.Item1, state.Value.RawValue);
            Assert.Empty(state.Value.Errors);

            Context.VerifySet(c => c.Result = ModelBindingResult.Success(ValidValue.Item2), Times.Once());
        }

        [Fact]
        public void ReadJson_WithNullReader_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "reader",
                () => Subject.ReadJson(null, typeof(TValue), null, JsonSerializer));
        }

        [Fact]
        public void WriteJson_WithNullWriter_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "writer",
                () => Subject.WriteJson(null, default(TValue), JsonSerializer));
        }
    }
}
