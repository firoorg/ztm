using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.Watching;
using Rule = Ztm.WebApi.TransactionConfirmationWatchers.Rule<Ztm.WebApi.TransactionConfirmationWatchers.CallbackResult>;

namespace Ztm.WebApi.TransactionConfirmationWatchers
{
    public interface IWatchRepository
    {
        Task AddAsync(TransactionWatch<Rule> watch, CancellationToken cancellationToken);
        Task<IEnumerable<TransactionWatch<Rule>>> ListAsync(WatchStatus status, CancellationToken cancellationToken);
        Task UpdateStatusAsync(Guid id, WatchStatus status, CancellationToken cancellationToken);
    }
}