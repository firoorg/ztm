using System;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface ITransactionConfirmationListener
    {
        Guid Id { get; }

        Task<bool> StartListenAsync(ZcoinTransaction transaction, CancellationToken cancellationToken);

        Task<bool> TransactionConfirmedAsync(ZcoinTransaction transaction, int confirmation);

        Task<bool> TransactionUnconfirmedAsync(ZcoinTransaction transaction, int confirmation);
    }
}
