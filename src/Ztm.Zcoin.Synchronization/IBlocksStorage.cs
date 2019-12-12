using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksStorage
    {
        Task AddAsync(Block block, int height, CancellationToken cancellationToken);

        Task<(Block block, int height)> GetAsync(uint256 hash, CancellationToken cancellationToken);

        Task<Block> GetAsync(int height, CancellationToken cancellationToken);

        Task<Block> GetFirstAsync(CancellationToken cancellationToken);

        Task<(Block block, int height)> GetLastAsync(CancellationToken cancellationToken);

        Task<Transaction> GetTransactionAsync(uint256 hash, CancellationToken cancellationToken);

        Task RemoveLastAsync(CancellationToken cancellationToken);
    }
}
