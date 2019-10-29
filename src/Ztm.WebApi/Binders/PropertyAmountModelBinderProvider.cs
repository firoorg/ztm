using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Binders
{
    public sealed class PropertyAmountModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(PropertyAmount))
            {
                return new BinderTypeModelBinder(typeof(PropertyAmountModelBinder));
            }

            return null;
        }
    }
}
