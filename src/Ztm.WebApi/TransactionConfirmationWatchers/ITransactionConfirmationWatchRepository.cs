using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.Watching;
using Rule = Ztm.WebApi.TransactionConfirmationWatchers.TransactionConfirmationWatchingRule<Ztm.WebApi.TransactionConfirmationWatchers.TransactionConfirmationCallbackResult>;

namespace Ztm.WebApi.TransactionConfirmationWatchers
{
    public interface ITransactionConfirmationWatchRepository
    {
        Task AddAsync(TransactionWatch<Rule> watch, CancellationToken cancellationToken);
        Task<IEnumerable<TransactionWatch<Rule>>> ListAsync(TransactionConfirmationWatchingWatchStatus status, CancellationToken cancellationToken);
        Task UpdateStatusAsync(Guid id, TransactionConfirmationWatchingWatchStatus status, CancellationToken cancellationToken);
    }
}