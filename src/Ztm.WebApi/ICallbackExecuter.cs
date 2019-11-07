using System;
using System.Threading.Tasks;

namespace Ztm.WebApi
{
    public interface ICallbackExecuter
    {
        Task Execute(Uri url, CallbackResult result);
    }
}