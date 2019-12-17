using Microsoft.Extensions.DependencyInjection;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers
{
    public static class IServiceCollectionExtensions
    {
        public static void AddExodusTransactionRetriever(this IServiceCollection service)
        {
            // Exodus transaction retrievers
            service.AddSingleton<IExodusTransactionRetriever, SimpleSendRetriever>();

            // Main retriever
            service.AddSingleton<ITransactionRetriever, TransactionRetriever>();
        }
    }
}