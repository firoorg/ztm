using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ztm.WebApi
{
    public sealed class ModelBinderProvider<T> : IModelBinderProvider
    {
        readonly IModelBinder binder;

        public ModelBinderProvider(IModelBinder binder)
        {
            if (binder == null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            this.binder = binder;
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(T))
            {
                return this.binder;
            }

            return null;
        }
    }
}
