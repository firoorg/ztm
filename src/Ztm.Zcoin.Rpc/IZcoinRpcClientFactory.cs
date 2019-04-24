using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Rpc
{
    public interface IZcoinRpcClientFactory
    {
        Task<IZcoinRpcClient> CreateRpcClientAsync(CancellationToken cancellationToken);
    }
}
