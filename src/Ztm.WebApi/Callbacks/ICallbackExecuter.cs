using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.WebApi.Callbacks
{
    public interface ICallbackExecuter
    {
        Task ExecuteAsync(Guid id, Uri url, CallbackResult result, CancellationToken cancellationToken);
    }
}