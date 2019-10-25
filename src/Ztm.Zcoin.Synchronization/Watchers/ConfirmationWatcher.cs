using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public abstract class ConfirmationWatcher<T> : Watcher<T> where T : Watch
    {
        readonly IConfirmationWatcherHandler<T> handler;
        readonly IBlocksStorage blocks;

        protected ConfirmationWatcher(IConfirmationWatcherHandler<T> handler, IBlocksStorage blocks) : base(handler)
        {
            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            this.handler = handler;
            this.blocks = blocks;
        }

        protected override async Task<bool> ExecuteMatchedWatchAsync(
            T watch,
            Block block,
            int height,
            BlockEventType blockEventType,
            CancellationToken cancellationToken)
        {
            // Get confirmation type.
            ConfirmationType confirmationType;

            switch (blockEventType)
            {
                case BlockEventType.Added:
                    confirmationType = ConfirmationType.Confirmed;
                    break;
                case BlockEventType.Removing:
                    confirmationType = ConfirmationType.Unconfirming;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(blockEventType),
                        blockEventType,
                        "The value is not supported."
                    );
            }

            // Load watching block.
            var currentHeight = height;

            (block, height) = await this.blocks.GetAsync(watch.StartBlock, cancellationToken);

            if (height > currentHeight)
            {
                throw new ArgumentException("The value is not a valid current height.", nameof(height));
            }

            // Invoke handler.
            var confirmation = currentHeight - height + 1;

            return await this.handler.ConfirmationUpdateAsync(watch, confirmation, confirmationType, cancellationToken);
        }

        protected override Task<IEnumerable<T>> GetWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return this.handler.GetCurrentWatchesAsync(cancellationToken);
        }
    }
}
