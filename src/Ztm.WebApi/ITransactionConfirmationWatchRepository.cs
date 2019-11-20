using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.WebApi
{
    public interface ITransactionConfirmationWatchRepository<TCallbackResult>
    {
        Task<TransactionConfirmationWatch<TCallbackResult>> AddAsync
        (
            uint256 transaction,
            int confirmation,
            TimeSpan unconfirmedWaitingTime,
            TCallbackResult successData,
            TCallbackResult timeoutData,
            Callback callback,
            CancellationToken cancellationToken
        );

        Task<TransactionConfirmationWatch<TCallbackResult>> GetAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<TransactionConfirmationWatch<TCallbackResult>>> ListAsync(CancellationToken cancellationToken);
        Task SetRemainingWaitingTimeAsync(Guid id, TimeSpan remainingTime, CancellationToken cancellationToken);
        Task CompleteAsync(Guid id, CancellationToken cancellationToken);
    }
}