using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.WebApi.Callbacks
{
    public interface ICallbackRepository
    {
        Task<Callback> AddAsync(IPAddress registeringIp, Uri url, CancellationToken cancellationToken);

        Task AddHistoryAsync(Guid id, CallbackResult result, CancellationToken cancellationToken);

        Task<Callback> GetAsync(Guid id, CancellationToken cancellationToken);

        Task SetCompletedAsyc(Guid id, CancellationToken cancellationToken);
    }
}
