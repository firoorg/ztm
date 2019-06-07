using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    [Guid("a17fd06a-ff3b-4cf7-af66-0c56ea77bc94")]
    public class TransactionConfirmationWatcher : IBlockConfirmationListener
    {
        readonly IMainDatabaseFactory db;
        readonly Dictionary<Guid, ITransactionConfirmationListener> listeners;

        public TransactionConfirmationWatcher(
            IMainDatabaseFactory db,
            params ITransactionConfirmationListener[] listeners)
        {
            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            if (listeners == null)
            {
                throw new ArgumentNullException(nameof(listeners));
            }

            this.db = db;
            this.listeners = listeners.ToDictionary(l => l.Id);
        }

        async Task RemoveWatchesAsync(IEnumerable<WatchingTransaction> watches, CancellationToken cancellationToken)
        {
            if (!watches.Any())
            {
                return;
            }

            using (var db = this.db.CreateDbContext())
            {
                db.WatchingTransactions.RemoveRange(watches);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        Guid IBlockConfirmationListener.Id => GetType().GUID;

        async Task<bool> IBlockConfirmationListener.BlockConfirmAsync(
            ZcoinBlock block,
            ConfirmationType type,
            int confirmation)
        {
            // Load watches.
            var transactions = block.Transactions.Cast<ZcoinTransaction>().ToDictionary(t => t.GetHash());
            WatchingTransaction[] watches;

            using (var db = this.db.CreateDbContext())
            {
                var hashes = transactions.Keys.ToArray();

                watches = await db.WatchingTransactions
                    .Where(t => hashes.Contains(t.Hash))
                    .ToArrayAsync(CancellationToken.None);
            }

            if (watches.Length == 0)
            {
                throw new ArgumentException("The block was not watched.", nameof(block));
            }

            // Invoke listeners.
            var watchesToRemove = new Collection<WatchingTransaction>();

            foreach (var watch in watches)
            {
                var listener = this.listeners[watch.Listener];
                var transaction = transactions[watch.Hash];

                var keep = await listener.TransactionConfirmAsync(transaction, type, confirmation);

                if (!keep)
                {
                    watchesToRemove.Add(watch);
                }
                else if (type == ConfirmationType.Unconfirming && confirmation == 1)
                {
                    throw new InvalidOperationException($"{listener.GetType()} want to continue watching transaction {transaction.GetHash()} but it is going to be removed now.");
                }
            }

            await RemoveWatchesAsync(watchesToRemove, CancellationToken.None);

            return watches.Length > watchesToRemove.Count;
        }

        async Task<bool> IBlockConfirmationListener.StartListenAsync(ZcoinBlock block, int height)
        {
            // Determine if we need to watch some transaction in this block.
            var watches = new Collection<WatchingTransaction>();

            foreach (ZcoinTransaction transaction in block.Transactions)
            {
                var hash = transaction.GetHash();

                foreach (var listener in this.listeners)
                {
                    var enable = await listener.Value.StartListenAsync(transaction);

                    if (enable)
                    {
                        watches.Add(new WatchingTransaction()
                        {
                            Hash = hash,
                            Listener = listener.Key,
                            StartTime = DateTime.UtcNow
                        });
                    }
                }
            }

            // Add watch to database if there is one.
            if (watches.Count > 0)
            {
                using (var db = this.db.CreateDbContext())
                {
                    await db.WatchingTransactions.AddRangeAsync(watches);
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                return true;
            }

            return false;
        }
    }
}
