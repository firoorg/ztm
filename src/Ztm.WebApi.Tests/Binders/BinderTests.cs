using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NSubstitute;

namespace Ztm.WebApi.Tests.Binders
{
    public abstract class BinderTests<T>
    {
        protected BinderTests()
        {
            Metadata = Substitute.ForPartsOf<ModelMetadata>(ModelMetadataIdentity.ForType(typeof(T)));
            ValueProvider = Substitute.For<IValueProvider>();

            Context = Substitute.ForPartsOf<ModelBindingContext>();
            Context.ModelMetadata = Metadata;
            Context.ModelState = new ModelStateDictionary();
            Context.ValueProvider = ValueProvider;
        }

        protected ModelBindingContext Context { get; }

        protected ModelMetadata Metadata { get; }

        protected IValueProvider ValueProvider { get; }
    }
}
