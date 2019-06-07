using System;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlockConfirmationListener
    {
        Guid Id { get; }

        Task<bool> BlockConfirmAsync(ZcoinBlock block, ConfirmationType type, int confirmation);

        Task<bool> StartListenAsync(ZcoinBlock block, int height);
    }
}
