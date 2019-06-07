using System;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface ITransactionConfirmationListener
    {
        Guid Id { get; }

        Task<bool> StartListenAsync(ZcoinTransaction transaction);

        Task<bool> TransactionConfirmAsync(ZcoinTransaction transaction, ConfirmationType type, int confirmation);
    }
}
