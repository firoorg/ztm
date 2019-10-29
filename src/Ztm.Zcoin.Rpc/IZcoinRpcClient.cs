using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public interface IZcoinRpcClient : IDisposable
    {
        Task<Transaction> CreateManagedPropertyAsync(
            BitcoinAddress owner,
            Ecosystem ecosystem,
            PropertyType type,
            PropertyId? currentId,
            string category,
            string subcategory,
            string name,
            string url,
            string description,
            CancellationToken cancellationToken);

        Task<Block> GetBlockAsync(uint256 blockHash, CancellationToken cancellationToken);

        Task<Block> GetBlockAsync(int height, CancellationToken cancellationToken);

        Task<BlockHeader> GetBlockHeaderAsync(uint256 blockHash, CancellationToken cancellationToken);

        Task<BlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken);

        Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken);

        Task<BitcoinAddress> GetNewAddressAsync(CancellationToken cancellationToken);

        Task<PropertyGrantsInfo> GetPropertyGrantsAsync(PropertyId id, CancellationToken cancellationToken);

        Task<Transaction> GrantPropertyAsync(
            PropertyId id,
            BitcoinAddress from,
            BitcoinAddress to,
            PropertyAmount amount,
            string note,
            CancellationToken cancellationToken);

        Task<IEnumerable<PropertyInfo>> ListPropertiesAsync(CancellationToken cancellationToken);

        Task<uint256> SendRawTransactionAsync(Transaction tx, CancellationToken cancellationToken);

        Task<uint256> SendToAddressAsync(
            BitcoinAddress address,
            Money amount,
            string comment,
            string commentTo,
            bool subtractFeeFromAmount,
            CancellationToken cancellationToken);
    }
}
