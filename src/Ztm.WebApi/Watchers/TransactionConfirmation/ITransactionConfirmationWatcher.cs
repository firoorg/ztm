using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public interface ITransactionConfirmationWatcher
    {
        Task<Rule> AddTransactionAsync(
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            Callback callback,
            CallbackResult successData,
            CallbackResult timeoutData,
            CancellationToken cancellationToken);
    }
}