using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksStorage
    {
        Task AddAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);

        Task<(ZcoinBlock block, int height)> GetAsync(uint256 hash, CancellationToken cancellationToken);

        Task<ZcoinBlock> GetAsync(int height, CancellationToken cancellationToken);

        Task<ZcoinBlock> GetFirstAsync(CancellationToken cancellationToken);

        Task<(ZcoinBlock block, int height)> GetLastAsync(CancellationToken cancellationToken);

        Task<ZcoinTransaction> GetTransactionAsync(uint256 hash, CancellationToken cancellationToken);

        Task RemoveLastAsync(CancellationToken cancellationToken);
    }
}
