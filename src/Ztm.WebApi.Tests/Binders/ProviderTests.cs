using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NSubstitute;

namespace Ztm.WebApi.Tests.Binders
{
    public abstract class ProviderTests<TProvider, TModel> where TProvider : IModelBinderProvider, new()
    {
        protected ProviderTests()
        {
            Metadata = Substitute.ForPartsOf<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(TModel)));
            Services = Substitute.For<IServiceProvider>();

            Context = Substitute.ForPartsOf<ModelBinderProviderContext>();
            Context.Metadata.Returns(Metadata);
            Context.Services.Returns(Services);

            Subject = new TProvider();
        }

        protected ModelBinderProviderContext Context { get; }

        protected ModelMetadata Metadata { get; }

        protected IServiceProvider Services { get; }

        protected TProvider Subject { get; }
    }
}
