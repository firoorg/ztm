using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.Watching;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public interface IWatchRepository
    {
        Task AddAsync(TransactionWatch<Rule> watch, CancellationToken cancellationToken);
        Task<IEnumerable<TransactionWatch<Rule>>> ListAsync(WatchStatus status, CancellationToken cancellationToken);
        Task UpdateStatusAsync(Guid id, WatchStatus status, CancellationToken cancellationToken);
    }
}