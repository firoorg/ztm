using System.Threading;
using System.Threading.Tasks;

namespace Ztm.Zcoin.Rpc
{
    public interface IRpcFactory
    {
        Task<IChainInformationRpc> CreateChainInformationRpcAsync(CancellationToken cancellationToken);
        Task<IExodusInformationRpc> CreateExodusInformationRpcAsync(CancellationToken cancellationToken);
        Task<IPropertyManagementRpc> CreatePropertyManagementRpcAsync(CancellationToken cancellationToken);
        Task<IRawTransactionRpc> CreateRawTransactionRpcAsync(CancellationToken cancellationToken);
        Task<IWalletRpc> CreateWalletRpcAsync(CancellationToken cancellationToken);
    }
}
