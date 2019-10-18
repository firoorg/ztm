using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace Ztm.WebApi
{
    public interface ICallbackRepository
    {
        Task<Callback> AddAsync(IPAddress ip, DateTime requestedTime, Uri url, CancellationToken cancellationToken);
        Task<Callback> SetStatusAsCompletedAsync(Guid id, CancellationToken cancellationToken);
        Task<Callback> GetAsync(Guid id, CancellationToken cancellationToken);
        Task AddInvocationAsync(Guid id, string status, DateTime invokedTime, byte[] data, CancellationToken cancellationToken);
    }
}