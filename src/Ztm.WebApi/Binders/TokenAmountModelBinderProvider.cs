using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.WebApi.Binders
{
    public class TokenAmountModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(TokenAmount))
            {
                return new BinderTypeModelBinder(typeof(TokenAmountModelBinder));
            }

            return null;
        }
    }
}
