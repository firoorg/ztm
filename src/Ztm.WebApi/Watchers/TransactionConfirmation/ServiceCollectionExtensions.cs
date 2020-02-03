using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ztm.Zcoin.Synchronization;
using IWatcher= Ztm.WebApi.Watchers.TransactionConfirmation.ITransactionConfirmationWatcher;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTransactionConfirmationWatcher(this IServiceCollection services)
        {
            services.AddSingleton<IRuleRepository, EntityRuleRepository>();
            services.AddSingleton<IWatchRepository, EntityWatchRepository>();
            services.AddSingleton<TransactionConfirmationWatcher>();
            services.AddSingleton<IBlockListener>(p => p.GetRequiredService<TransactionConfirmationWatcher>());
            services.AddSingleton<IHostedService>(p => p.GetRequiredService<TransactionConfirmationWatcher>());
            services.AddSingleton<IWatcher>(p => p.GetRequiredService<TransactionConfirmationWatcher>());
        }
    }
}
