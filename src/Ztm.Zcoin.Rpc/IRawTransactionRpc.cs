using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public interface IRawTransactionRpc : IDisposable
    {
        Task<uint256> SendAsync(Transaction tx, CancellationToken cancellationToken);
    }
}
