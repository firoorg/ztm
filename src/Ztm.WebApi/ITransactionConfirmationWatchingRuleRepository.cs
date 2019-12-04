using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.WebApi
{
    public interface ITransactionConfirmationWatchingRuleRepository<TCallbackResult>
    {
        Task<TransactionConfirmationWatchingRule<TCallbackResult>> AddAsync
        (
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            TCallbackResult successData,
            TCallbackResult timeoutData,
            Callback callback,
            CancellationToken cancellationToken
        );

        Task<TransactionConfirmationWatchingRule<TCallbackResult>> GetAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<TransactionConfirmationWatchingRule<TCallbackResult>>> ListActiveAsync(CancellationToken cancellationToken);
        Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan remainingTime, CancellationToken cancellationToken);
        Task<TimeSpan> GetRemainingWaitingTimeAsync(Guid id, CancellationToken cancellationToken);
        Task UpdateStatusAsync(Guid id, TransactionConfirmationWatchingRuleStatus status, CancellationToken cancellationToken);
    }
}