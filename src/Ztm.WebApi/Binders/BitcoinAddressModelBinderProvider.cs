using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;

namespace Ztm.WebApi.Binders
{
    public sealed class BitcoinAddressModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(BitcoinAddress))
            {
                var network = context.Services.GetRequiredService<Network>();

                return new BitcoinAddressModelBinder(network);
            }

            return null;
        }
    }
}
