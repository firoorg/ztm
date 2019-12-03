using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ztm.Hosting.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        public static void AddBackgroundServiceExceptionHandler(this IServiceCollection services)
        {
            services.AddSingleton<BackgroundServiceExceptionHandler>();
            services.AddSingleton<IBackgroundServiceExceptionHandler>(
                p => p.GetRequiredService<BackgroundServiceExceptionHandler>()
            );
            services.AddSingleton<IBackgroundServiceErrorCollector>(
                p => p.GetRequiredService<BackgroundServiceExceptionHandler>().Collector
            );
        }

        public static void UseBackgroundServiceExceptionHandler(this IApplicationBuilder app, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            app.Use((context, next) =>
            {
                var handler = context.RequestServices.GetRequiredService<IBackgroundServiceErrorCollector>();

                if (handler.Any())
                {
                    var feature = new BackgroundServiceExceptionHandlerFeature(handler);

                    context.Features.Set<IBackgroundServiceExceptionHandlerFeature>(feature);

                    context.Request.Path = path;
                }

                return next();
            });
        }
    }
}
