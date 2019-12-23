using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public interface IRuleRepository
    {
        Task<Rule> AddAsync
        (
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            CallbackResult successResponse,
            CallbackResult timeoutResponse,
            Callback callback,
            CancellationToken cancellationToken
        );

        Task<Rule> GetAsync(Guid id, CancellationToken cancellationToken);
        Task<TimeSpan> GetRemainingWaitingTimeAsync(Guid id, CancellationToken cancellationToken);
        Task<RuleStatus> GetStatusAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Rule>> ListWaitingAsync(CancellationToken cancellationToken);
        Task SubtractRemainingWaitingTimeAsync(Guid id, TimeSpan consumedTime, CancellationToken cancellationToken);
        Task UpdateCurrentWatchAsync(Guid id, Guid? watchId, CancellationToken cancellationToken);
        Task UpdateStatusAsync(Guid id, RuleStatus status, CancellationToken cancellationToken);
    }
}