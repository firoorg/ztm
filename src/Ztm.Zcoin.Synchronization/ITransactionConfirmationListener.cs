using System;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface ITransactionConfirmationListener
    {
        Guid Id { get; }

        Task<bool> StartListenAsync(ZcoinTransaction transaction);

        Task<bool> TransactionConfirmedAsync(ZcoinTransaction transaction, int confirmation);

        Task<bool> TransactionUnconfirmedAsync(ZcoinTransaction transaction, int confirmation);
    }
}
