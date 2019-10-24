using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksRetrieverHandler
    {
        Task<int> GetBlockHintAsync(CancellationToken cancellationToken);

        Task<int> ProcessBlockAsync(Block block, int height, CancellationToken cancellationToken);
    }
}
