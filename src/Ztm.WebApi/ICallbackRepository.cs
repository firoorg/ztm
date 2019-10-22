using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace Ztm.WebApi
{
    public interface ICallbackRepository
    {
        Task<Callback> AddAsync(IPAddress ip, Uri url, CancellationToken cancellationToken);
        Task SetCompletedAsyc(Guid id, CancellationToken cancellationToken);
        Task<Callback> GetAsync(Guid id, CancellationToken cancellationToken);
        Task AddHistoryAsync(Guid id, string status, string data, CancellationToken cancellationToken);
    }
}