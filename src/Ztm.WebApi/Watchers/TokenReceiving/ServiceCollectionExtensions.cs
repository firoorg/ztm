using Microsoft.Extensions.DependencyInjection;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTokenReceivingWatcher(this IServiceCollection services)
        {
            services.AddSingleton<IRuleRepository, EntityRuleRepository>();
            services.AddSingleton<IWatchRepository, EntityWatchRepository>();
            services.AddSingleton<TokenReceivingWatcher>();
            services.AddSingleton<ITokenReceivingWatcher>(p => p.GetRequiredService<TokenReceivingWatcher>());
        }
    }
}
