using System;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;

namespace Ztm.Zcoin.NBitcoin.RPC
{
    public class ZcoinRPCClient : RPCClient
    {
        public ZcoinRPCClient(RPCCredentialString credentials, Uri address, Network network = null)
            : base(credentials, address, network)
        {
        }

        public ZcoinRPCClient(string authenticationString, Uri address, Network network = null)
            : base(authenticationString, address, network)
        {
        }

        public new async Task<ZcoinBlock> GetBlockAsync(uint256 blockId)
        {
            return (ZcoinBlock)await base.GetBlockAsync(blockId);
        }

        public new async Task<ZcoinBlock> GetBlockAsync(int height)
        {
            return (ZcoinBlock)await base.GetBlockAsync(height);
        }

        public new async Task<ZcoinBlockHeader> GetBlockHeaderAsync(uint256 blockHash)
        {
            return (ZcoinBlockHeader)await base.GetBlockHeaderAsync(blockHash);
        }

        public new async Task<ZcoinBlockHeader> GetBlockHeaderAsync(int height)
        {
            return (ZcoinBlockHeader)await base.GetBlockHeaderAsync(height);
        }
    }
}
