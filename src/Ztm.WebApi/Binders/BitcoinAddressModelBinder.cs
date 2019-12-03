using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NBitcoin;

namespace Ztm.WebApi.Binders
{
    public sealed class BitcoinAddressModelBinder : IModelBinder
    {
        readonly Network network;

        public BitcoinAddressModelBinder(Network network)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            this.network = network;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // Retrieve submitted value.
            var name = bindingContext.ModelName;
            var values = bindingContext.ValueProvider.GetValue(name);

            if (values == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(name, values);

            var value = values.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            // Convert to domain object.
            BitcoinAddress model;

            try
            {
                model = BitcoinAddress.Create(value, this.network);
            }
            catch (Exception ex) // lgtm[cs/catch-of-all-exceptions]
            {
                bindingContext.ModelState.AddModelError(name, ex, bindingContext.ModelMetadata);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
