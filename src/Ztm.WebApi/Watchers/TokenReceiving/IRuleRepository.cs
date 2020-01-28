using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public interface IRuleRepository
    {
        Task AddAsync(Rule rule, CancellationToken cancellationToken);

        Task DecreaseTimeoutAsync(Guid id, TimeSpan value, CancellationToken cancellationToken);

        Task<Rule> GetAsync(Guid id, CancellationToken cancellationToken);

        Task<TimeSpan> GetCurrentTimeoutAsync(Guid id, CancellationToken cancellationToken);

        Task<IEnumerable<Rule>> ListUncompletedAsync(PropertyId property, CancellationToken cancellationToken);

        Task SetSucceededAsync(Guid id, CancellationToken cancellationToken);

        Task SetTimedOutAsync(Guid id, CancellationToken cancellationToken);
    }
}
