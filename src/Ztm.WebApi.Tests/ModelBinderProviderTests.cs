using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
using Xunit;

namespace Ztm.WebApi.Tests
{
    public sealed class ModelBinderProviderTests
    {
        readonly Mock<ModelBinderProviderContext> context;
        readonly Mock<IModelBinder> binder;
        readonly ModelBinderProvider<string> subject;

        public ModelBinderProviderTests()
        {
            this.context = new Mock<ModelBinderProviderContext>();
            this.binder = new Mock<IModelBinder>();
            this.subject = new ModelBinderProvider<string>(this.binder.Object);
        }

        [Fact]
        public void Constructor_WithNullBinder_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("binder", () => new ModelBinderProvider<object>(null));
        }

        [Fact]
        public void GetBinder_WithNullContext_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("context", () => this.subject.GetBinder(null));
        }

        [Fact]
        public void GetBinder_WithMatchedModelType_ShouldReturnBinder()
        {
            // Arrange.
            var meta = new Mock<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(string)));

            this.context.SetupGet(c => c.Metadata).Returns(meta.Object);

            // Act.
            var result = this.subject.GetBinder(this.context.Object);

            // Assert.
            Assert.Same(this.binder.Object, result);
        }

        [Fact]
        public void GetBinder_WithNonMatchedModelType_ShouldReturnNull()
        {
            // Arrange.
            var meta = new Mock<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(int)));

            this.context.SetupGet(c => c.Metadata).Returns(meta.Object);

            // Act.
            var result = this.subject.GetBinder(this.context.Object);

            // Assert.
            Assert.Null(result);
        }
    }
}
