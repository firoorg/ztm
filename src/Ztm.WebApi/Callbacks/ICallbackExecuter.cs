using System;
using System.Threading.Tasks;

namespace Ztm.WebApi.Callbacks
{
    public interface ICallbackExecuter
    {
        Task Execute(Guid id, Uri url, CallbackResult result);
    }
}