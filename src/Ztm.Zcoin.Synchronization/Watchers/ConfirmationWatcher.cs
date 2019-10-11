using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers
{
    public abstract class ConfirmationWatcher<T> : Watcher<T> where T : Watch
    {
        readonly IConfirmationWatcherStorage<T> storage;
        readonly IBlocksStorage blocks;

        protected ConfirmationWatcher(IConfirmationWatcherStorage<T> storage, IBlocksStorage blocks) : base(storage)
        {
            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            this.storage = storage;
            this.blocks = blocks;
        }

        protected abstract Task<bool> ConfirmAsync(
            T watch,
            ConfirmationType type,
            int confirmation,
            CancellationToken cancellationToken);

        protected override async Task<bool> ExecuteMatchedWatchAsync(
            T watch,
            ZcoinBlock block,
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

            return await ConfirmAsync(watch, confirmationType, confirmation, cancellationToken);
        }

        protected override Task<IEnumerable<T>> GetWatchesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken)
        {
            return this.storage.GetWatchesAsync(cancellationToken);
        }
    }
}
