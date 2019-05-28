using System;
using System.Threading;
using Ztm.ObjectModel;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public class BlockEventArgs : AsyncEventArgs
    {
        public BlockEventArgs(ZcoinBlock block, int height, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "The value is negative.");
            }

            Block = block;
            Height = height;
        }

        public ZcoinBlock Block { get; }

        public int Height { get; }
    }
}
