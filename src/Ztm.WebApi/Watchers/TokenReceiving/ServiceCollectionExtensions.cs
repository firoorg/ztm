using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ztm.Zcoin.Synchronization;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTokenReceivingWatcher(this IServiceCollection services)
        {
            services.AddSingleton<IRuleRepository, EntityRuleRepository>();
            services.AddSingleton<IWatchRepository, EntityWatchRepository>();
            services.AddSingleton<TokenReceivingWatcher>();
            services.AddSingleton<IBlockListener>(p => p.GetRequiredService<TokenReceivingWatcher>());
            services.AddSingleton<IHostedService>(p => p.GetRequiredService<TokenReceivingWatcher>());
            services.AddSingleton<ITokenReceivingWatcher>(p => p.GetRequiredService<TokenReceivingWatcher>());
        }
    }
}
