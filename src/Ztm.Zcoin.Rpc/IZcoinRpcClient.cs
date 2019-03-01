using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Rpc
{
    public interface IZcoinRpcClient : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken);
    }
}
