using Microsoft.Extensions.DependencyInjection;

namespace Ztm.WebApi.AddressPools
{
    public static class IServiceCollectionExtensions
    {
        public static void UseAddressPool(this IServiceCollection services)
        {
            services.AddSingleton<IAddressChoser, LessUsageFirstChoser>();
            services.AddSingleton<IAddressGenerator, RpcAddressGenerator>();
            services.AddSingleton<IReceivingAddressStorage, EntityReceivingAddressStorage>();

            services.AddSingleton<IReceivingAddressPool, ReceivingAddressPool>();
        }
    }
}