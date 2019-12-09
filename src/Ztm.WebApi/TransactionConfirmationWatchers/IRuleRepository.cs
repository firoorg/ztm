using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.TransactionConfirmationWatchers
{
    public interface IRuleRepository<TCallbackResult>
    {
        Task<Rule<TCallbackResult>> AddAsync
        (
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            TCallbackResult successData,
            TCallbackResult timeoutData,
            Callback callback,
            CancellationToken cancellationToken
        );

        Task<Rule<TCallbackResult>> GetAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Rule<TCallbackResult>>> ListActiveAsync(CancellationToken cancellationToken);
        Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan remainingTime, CancellationToken cancellationToken);
        Task<TimeSpan> GetRemainingWaitingTimeAsync(Guid id, CancellationToken cancellationToken);
        Task UpdateStatusAsync(Guid id, RuleStatus status, CancellationToken cancellationToken);
    }
}