using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksStorage
    {
        Task AddAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);

        Task<ZcoinBlock> GetAsync(uint256 hash, CancellationToken cancellationToken);

        Task<ZcoinBlock> GetAsync(int height, CancellationToken cancellationToken);

        Task<ZcoinBlock> GetFirstAsync(CancellationToken cancellationToken);

        Task<ZcoinBlock> GetLastAsync(CancellationToken cancellationToken);

        Task RemoveAsync(uint256 hash, CancellationToken cancellationToken);

        Task RemoveAsync(int height, CancellationToken cancellationToken);
    }
}
