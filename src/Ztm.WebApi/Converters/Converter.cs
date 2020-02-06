using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Ztm.WebApi.Converters
{
    public abstract class Converter<T> : JsonConverter<T>, IModelBinder
    {
        protected Converter()
        {
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
            T model;

            try
            {
                model = Parse(value);
            }
            catch (Exception ex) // lgtm[cs/catch-of-all-exceptions]
            {
                bindingContext.ModelState.AddModelError(name, ex, bindingContext.ModelMetadata);
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }

        protected abstract T Parse(string s);
    }
}
