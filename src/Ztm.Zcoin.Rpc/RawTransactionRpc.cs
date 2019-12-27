using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;

namespace Ztm.Zcoin.Rpc
{
    public sealed class RawTransactionRpc : RpcClient, IRawTransactionRpc
    {
        public RawTransactionRpc(RpcFactory factory, RPCClient client) : base(factory, client)
        {
        }

        public Task<uint256> SendAsync(Transaction tx, CancellationToken cancellationToken)
        {
            if (tx == null)
            {
                throw new ArgumentNullException(nameof(tx));
            }

            return Client.SendRawTransactionAsync(tx);
        }
    }
}
