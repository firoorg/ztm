using Microsoft.Extensions.DependencyInjection;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers
{
    public static class IServiceCollectionExtensions
    {
        public static void AddExodusTransactionRetriever(this IServiceCollection service)
        {
            // Exodus transaction interpreters
            service.AddSingleton<IExodusTransactionRetriever, SimpleSendRetriever>();

            // Main interpreter
            service.AddSingleton<ITransactionRetriever, TransactionRetriever>();
        }
    }
}