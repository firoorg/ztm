using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public class BlockConfirmationWatcher : IBlockListener
    {
        readonly IMainDatabaseFactory db;
        readonly IBlocksStorage blocks;
        readonly Dictionary<Guid, IBlockConfirmationListener> listeners;

        public BlockConfirmationWatcher(
            IMainDatabaseFactory db,
            IBlocksStorage blocks,
            params IBlockConfirmationListener[] listeners)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            if (listeners == null)
            {
                throw new ArgumentNullException(nameof(listeners));
            }

            this.db = db;
            this.blocks = blocks;
            this.listeners = listeners.ToDictionary(l => l.Id);
        }

        async Task InvokeListenersAsync(
            IEnumerable<WatchingBlock> watches,
            ZcoinBlock currentBlock,
            int currentHeight,
            ConfirmationType type)
        {
            // Invoke all listeners.
            var watchesToRemove = new Collection<WatchingBlock>();
            var cachedBlocks = new Dictionary<uint256, Tuple<ZcoinBlock, int>>()
            {
                { currentBlock.GetHash(), Tuple.Create(currentBlock, currentHeight )}
            };

            foreach (var watch in watches)
            {
                // Get block.
                Tuple<ZcoinBlock, int> cached;
                ZcoinBlock block;
                int height;

                if (cachedBlocks.TryGetValue(watch.Hash, out cached))
                {
                    block = cached.Item1;
                    height = cached.Item2;
                }
                else
                {
                    (block, height) = await this.blocks.GetAsync(watch.Hash, CancellationToken.None);
                    cachedBlocks.Add(watch.Hash, Tuple.Create(block, height));
                }

                if (height > currentHeight)
                {
                    throw new ArgumentException("The value is not valid current height.", nameof(currentHeight));
                }

                // Invoke listener.
                if (!await InvokeListenerAsync(currentHeight, this.listeners[watch.Listener], block, height, type))
                {
                    watchesToRemove.Add(watch);
                }
            }

            // Remove stopped watch.
            if (watchesToRemove.Count > 0)
            {
                using (var db = this.db.CreateDbContext())
                {
                    db.WatchingBlocks.RemoveRange(watchesToRemove);
                    await db.SaveChangesAsync(CancellationToken.None);
                }
            }
        }

        async Task<bool> InvokeListenerAsync(
            int currentHeight,
            IBlockConfirmationListener listener,
            ZcoinBlock block,
            int height,
            ConfirmationType type)
        {
            int confirmation;
            bool continueWatching;

            switch (type)
            {
                case ConfirmationType.Confirmed:
                    confirmation = currentHeight - height + 1;
                    continueWatching = await listener.BlockConfirmedAsync(block, confirmation);
                    break;
                case ConfirmationType.Unconfirmed:
                    confirmation = currentHeight - height;
                    continueWatching = await listener.BlockUnconfirmedAsync(block, confirmation);
                    break;
                default:
                    throw new NotImplementedException($"Confirmation type {type} was not implemented.");
            }

            return continueWatching && confirmation > 0;
        }

        async Task IBlockListener.BlockAddedAsync(ZcoinBlock block, int height)
        {
            var hash = block.GetHash();

            // Determine new watches.
            var watches = new Collection<WatchingBlock>();

            foreach (var listener in this.listeners)
            {
                var enable = await listener.Value.StartListenAsync(block, height);

                if (enable)
                {
                    watches.Add(new WatchingBlock()
                    {
                        Hash = hash,
                        Listener = listener.Key,
                        StartTime = DateTime.UtcNow
                    });
                }
            }

            // Add new watches and load previous watches.
            using (var db = this.db.CreateDbContext())
            {
                var previous = await db.WatchingBlocks.ToArrayAsync(CancellationToken.None);

                await db.WatchingBlocks.AddRangeAsync(watches, CancellationToken.None);

                foreach (var watch in previous)
                {
                    watches.Add(watch);
                }

                await db.SaveChangesAsync(CancellationToken.None);
            }

            // Invoke listeners.
            await InvokeListenersAsync(watches, block, height, ConfirmationType.Confirmed);
        }

        async Task IBlockListener.BlockRemovedAsync(ZcoinBlock block, int height)
        {
            // Load current watches.
            IEnumerable<WatchingBlock> watches;

            using (var db = this.db.CreateDbContext())
            {
                watches = await db.WatchingBlocks.ToArrayAsync(CancellationToken.None);
            }

            // Invoke listeners.
            await InvokeListenersAsync(watches, block, height, ConfirmationType.Unconfirmed);
        }

        enum ConfirmationType
        {
            Confirmed,
            Unconfirmed
        }
    }
}
