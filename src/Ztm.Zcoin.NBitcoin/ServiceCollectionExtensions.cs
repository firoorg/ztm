using Microsoft.Extensions.DependencyInjection;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin
{
    public static class ServiceCollectionExtensions
    {
        public static void AddNBitcoin(this IServiceCollection services)
        {
            // Payload Encoders
            services.AddSingleton<ITransactionPayloadEncoder, SimpleSendEncoder>();

            // Encoder
            services.AddSingleton<ITransactionEncoder, TransactionEncoder>();
        }
    }
}