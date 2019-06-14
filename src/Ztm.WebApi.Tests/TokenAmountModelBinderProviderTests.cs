using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NSubstitute;
using Xunit;
using Ztm.WebApi.Binders;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.WebApi.Tests
{
    public class TokenAmountModelBinderProviderTests
    {
        readonly ModelMetadata meta;
        readonly ModelBinderProviderContext context;
        readonly TokenAmountModelBinderProvider subject;

        public TokenAmountModelBinderProviderTests()
        {
            this.meta = Substitute.ForPartsOf<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(TokenAmount)));
            this.context = Substitute.ForPartsOf<ModelBinderProviderContext>();
            this.context.Metadata.Returns(this.meta);
            this.subject = new TokenAmountModelBinderProvider();
        }

        [Fact]
        public void GetBinder_WithNullContext_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "context",
                () => this.subject.GetBinder(null)
            );
        }

        [Fact]
        public void GetBinder_ModelTypeIsTokenAmount_ShouldReturnNonNull()
        {
            var binder = this.subject.GetBinder(this.context);

            Assert.NotNull(binder);
        }
    }
}
