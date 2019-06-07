using System;
using Ztm.ServiceModel;

namespace Ztm.Zcoin.Synchronization
{
    public interface IBlocksSynchronizer : IBackgroundService
    {
        event EventHandler<BlockEventArgs> BlockAdded;

        event EventHandler<BlockEventArgs> BlockRemoving;
    }
}
