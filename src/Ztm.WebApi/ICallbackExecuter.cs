using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.WebApi
{
    public interface ICallbackExecuter
    {
        Task Execute(Guid id, Uri url, CallbackResult result);
    }
}