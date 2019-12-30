using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Watch = Ztm.Zcoin.Watching.TransactionWatch<Ztm.WebApi.Watchers.TransactionConfirmation.Rule>;

namespace Ztm.WebApi.Watchers.TransactionConfirmation
{
    public interface IWatchRepository
    {
        Task AddAsync(Watch watch, CancellationToken cancellationToken);
        Task<IEnumerable<Watch>> ListPendingAsync(uint256 startBlock, CancellationToken cancellationToken);
        Task<IEnumerable<Watch>> ListRejectedAsync(uint256 startBlock, CancellationToken cancellationToken);
        Task<IEnumerable<Watch>> ListSucceededAsync(uint256 startBlock, CancellationToken cancellationToken);
        Task SetRejectedAsync(Guid id, CancellationToken cancellationToken);
        Task SetSucceededAsync(Guid id, CancellationToken cancellationToken);
    }
}
