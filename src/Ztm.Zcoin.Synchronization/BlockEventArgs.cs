using System;
using System.Threading;
using Ztm.ObjectModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public class BlockEventArgs : AsyncEventArgs
    {
        public BlockEventArgs(ZcoinBlock block, CancellationToken cancellationToken) : base(cancellationToken)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            Block = block;
        }

        public ZcoinBlock Block { get; }
    }
}
