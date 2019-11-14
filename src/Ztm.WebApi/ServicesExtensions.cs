using Microsoft.Extensions.DependencyInjection;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi
{
    public static class ServicesExtensions
    {
        public static void AddTransactionEncoder(this IServiceCollection services)
        {
            // Payload Encoders
            services.AddTransient<ITransactionPayloadEncoder, SimpleSendEncoder>();

            // Encoder
            services.AddTransient<ITransactionEncoder, TransactionEncoder>();
        }
    }
}