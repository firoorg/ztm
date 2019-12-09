using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.Callbacks;
using Rule = Ztm.WebApi.TransactionConfirmationWatchers.TransactionConfirmationWatchingRule<Ztm.WebApi.TransactionConfirmationWatchers.TransactionConfirmationCallbackResult>;

namespace Ztm.WebApi.TransactionConfirmationWatchers
{
    public interface ITransactionConfirmationWatcher
    {
        Task<Rule> AddTransactionAsync(
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            Callback callback,
            TransactionConfirmationCallbackResult successData,
            TransactionConfirmationCallbackResult timeoutData,
            CancellationToken cancellationToken);
    }
}