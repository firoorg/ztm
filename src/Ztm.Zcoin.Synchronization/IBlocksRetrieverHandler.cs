using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksRetrieverHandler
    {
        Task DiscardBlocksAsync(int start, CancellationToken cancellationToken);

        Task<int> GetStartBlockAsync(CancellationToken cancellationToken);

        Task<int> ProcessBlockAsync(Block block, int height, CancellationToken cancellationToken);
    }
}
