using Microsoft.Extensions.DependencyInjection;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers;

namespace Ztm.Zcoin.NBitcoin
{
    public static class ServiceCollectionExtensions
    {
        public static void AddNBitcoin(this IServiceCollection services)
        {
            // Exodus Encoder.
            services.AddSingleton<ITransactionPayloadEncoder, SimpleSendEncoder>();
            services.AddSingleton<ITransactionEncoder, TransactionEncoder>();

            // Exodus Transaction Retriever.
            services.AddSingleton<IExodusTransactionRetriever, SimpleSendRetriever>();
            services.AddSingleton<ITransactionRetriever, TransactionRetriever>();
        }
    }
}
