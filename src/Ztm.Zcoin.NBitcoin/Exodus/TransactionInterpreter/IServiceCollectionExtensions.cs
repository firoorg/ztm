using Microsoft.Extensions.DependencyInjection;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public static class IServiceCollectionExtensions
    {
        public static void AddExodusTransactionInterpreter(this IServiceCollection service)
        {
            // Exodus transaction interpreters
            service.AddTransient<IExodusInterpreter, SimpleSendInterpreter>();

            // Main interpreter
            service.AddSingleton<IInterpreter, Interpreter>();
        }
    }
}