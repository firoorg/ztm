using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.WebApi
{
    using ConfirmContext = TransactionConfirmationWatch<TransactionConfirmationCallbackResult>;

    public interface ITransactionConfirmationWatcher
    {
        Task<ConfirmContext> AddTransactionAsync(
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            Callback callback,
            TransactionConfirmationCallbackResult successData,
            TransactionConfirmationCallbackResult timeoutData,
            CancellationToken cancellationToken);
    }
}