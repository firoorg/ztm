using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;

namespace Ztm.Zcoin.Rpc
{
    public sealed class ChainInformationRpc : RpcClient, IChainInformationRpc
    {
        public ChainInformationRpc(RpcFactory factory, RPCClient client) : base(factory, client)
        {
        }

        public async Task<Block> GetBlockAsync(uint256 hash, CancellationToken cancellationToken)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            var block = await Client.GetBlockAsync(hash);

            foreach (var tx in block.Transactions)
            {
                await PopulateExodusInformationAsync(tx, cancellationToken);
            }

            return block;
        }

        public async Task<Block> GetBlockAsync(int height, CancellationToken cancellationToken)
        {
            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "The value is not a valid height.");
            }

            var block = await Client.GetBlockAsync(height);

            foreach (var tx in block.Transactions)
            {
                await PopulateExodusInformationAsync(tx, cancellationToken);
            }

            return block;
        }

        public Task<BlockHeader> GetBlockHeaderAsync(uint256 hash, CancellationToken cancellationToken)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            return Client.GetBlockHeaderAsync(hash);
        }

        public Task<BlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken)
        {
            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "The value is not a valid height.");
            }

            return Client.GetBlockHeaderAsync(height);
        }

        public Task<BlockchainInfo> GetChainInfoAsync(CancellationToken cancellationToken)
        {
            return Client.GetBlockchainInfoAsync();
        }
    }
}
