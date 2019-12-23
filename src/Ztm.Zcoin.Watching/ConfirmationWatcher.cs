using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.Synchronization;

namespace Ztm.Zcoin.Watching
{
    public abstract class ConfirmationWatcher<TContext, TWatch, TConfirm> : Watcher<TContext, TWatch>
        where TWatch : Watch<TContext>
    {
        readonly IConfirmationWatcherHandler<TContext, TWatch, TConfirm> handler;
        readonly IBlocksStorage blocks;

        protected ConfirmationWatcher(
            IConfirmationWatcherHandler<TContext, TWatch, TConfirm> handler,
            IBlocksStorage blocks) : base(handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            this.handler = handler;
            this.blocks = blocks;
        }

        protected static ConfirmationType GetConfirmationType(BlockEventType eventType)
        {
            switch (eventType)
            {
                case BlockEventType.Added:
                    return ConfirmationType.Confirmed;
                case BlockEventType.Removing:
                    return ConfirmationType.Unconfirming;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(eventType),
                        eventType,
                        "The value is not a valid event type."
                    );
            }
        }

        protected async Task<int> GetConfirmationAsync(
            TWatch watch,
            int currentHeight,
            CancellationToken cancellationToken)
        {
            if (watch == null)
            {
                throw new ArgumentNullException(nameof(watch));
            }

            var (_, height) = await this.blocks.GetAsync(watch.StartBlock, cancellationToken);

            if (height > currentHeight)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentHeight),
                    currentHeight,
                    "The value is not a valid current height."
                );
            }

            return currentHeight - height + 1;
        }

        protected override Task<IEnumerable<TWatch>> GetWatchesAsync(
            Block block,
            int height,
            CancellationToken cancellationToken)
        {
            return this.handler.GetCurrentWatchesAsync(cancellationToken);
        }
    }
}
