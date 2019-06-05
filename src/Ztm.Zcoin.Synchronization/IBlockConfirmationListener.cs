using System;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockConfirmationListener
    {
        Guid Id { get; }

        Task<bool> BlockConfirmedAsync(ZcoinBlock block, int confirmation);

        Task<bool> BlockUnconfirmedAsync(ZcoinBlock block, int confirmation);

        Task<bool> StartListenAsync(ZcoinBlock block, int height, CancellationToken cancellationToken);
    }
}
