using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public interface IZcoinRpcClient : IDisposable
    {
        Task<Transaction> CreateManagedTokenAsync(
            BitcoinAddress owner,
            TokenEcosystem ecosystem,
            TokenType type,
            TokenId? currentId,
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

        Task<TokenGrantsInfo> GetTokenGrantsAsync(TokenId token, CancellationToken cancellationToken);

        Task<Transaction> GrantTokensAsync(
            TokenId id,
            BitcoinAddress from,
            BitcoinAddress to,
            TokenAmount amount,
            string note,
            CancellationToken cancellationToken);

        Task<IEnumerable<TokenInfo>> ListTokensAsync(CancellationToken cancellationToken);

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
