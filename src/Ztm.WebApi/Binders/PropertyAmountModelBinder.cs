using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Ztm.Configuration;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Binders
{
    public sealed class PropertyAmountModelBinder : IModelBinder
    {
        readonly ZcoinPropertyConfiguration config;

        public PropertyAmountModelBinder(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config.GetZcoinSection().Property;
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

            // Convert to domain object. We cannot use TokenAmount.Parse because we want to allow user to specify
            // integer for divisible.
            PropertyAmount model;

            try
            {
                switch (this.config.Type)
                {
                    case PropertyType.Divisible:
                        model = PropertyAmount.FromDivisible(decimal.Parse(value));
                        break;
                    case PropertyType.Indivisible:
                        model = new PropertyAmount(long.Parse(value));
                        break;
                    default:
                        throw new InvalidOperationException("The configuration for binder is not valid.");
                }
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
