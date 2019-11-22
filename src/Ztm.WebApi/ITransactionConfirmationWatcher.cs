using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Rule = Ztm.WebApi.TransactionConfirmationWatchingRule<Ztm.WebApi.TransactionConfirmationCallbackResult>;

namespace Ztm.WebApi
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