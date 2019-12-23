using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace Ztm.WebApi.Callbacks
{
    public interface ICallbackRepository
    {
        Task<Callback> AddAsync(IPAddress registeringIp, Uri url, CancellationToken cancellationToken);
        Task SetCompletedAsyc(Guid id, CancellationToken cancellationToken);
        Task<Callback> GetAsync(Guid id, CancellationToken cancellationToken);
        Task AddHistoryAsync(Guid id, CallbackResult result, CancellationToken cancellationToken);
    }
}