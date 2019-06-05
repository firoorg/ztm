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

        async Task<bool> InvokeListenersAsync(ZcoinBlock block, int confirmation, ConfirmationType type)
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
                bool keep;

                switch (type)
                {
                    case ConfirmationType.Confirmed:
                        keep = await listener.TransactionConfirmedAsync(transaction, confirmation);
                        break;
                    case ConfirmationType.Unconfirmed:
                        keep = await listener.TransactionUnconfirmedAsync(transaction, confirmation);
                        break;
                    default:
                        throw new ArgumentException($"Confirmation type {type} is not supported.", nameof(type));
                }

                if (!keep || confirmation == 0)
                {
                    watchesToRemove.Add(watch);
                }
            }

            await RemoveWatchesAsync(watchesToRemove, CancellationToken.None);

            return watches.Length > watchesToRemove.Count;
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

        Task<bool> IBlockConfirmationListener.BlockConfirmedAsync(ZcoinBlock block, int confirmation)
        {
            return InvokeListenersAsync(block, confirmation, ConfirmationType.Confirmed);
        }

        Task<bool> IBlockConfirmationListener.BlockUnconfirmedAsync(ZcoinBlock block, int confirmation)
        {
            return InvokeListenersAsync(block, confirmation, ConfirmationType.Unconfirmed);
        }

        async Task<bool> IBlockConfirmationListener.StartListenAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken)
        {
            // Determine if we need to watch some transaction in this block.
            var watches = new Collection<WatchingTransaction>();

            foreach (ZcoinTransaction transaction in block.Transactions)
            {
                var hash = transaction.GetHash();

                foreach (var listener in this.listeners)
                {
                    var enable = await listener.Value.StartListenAsync(transaction, cancellationToken);

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
                    await db.SaveChangesAsync(cancellationToken);
                }

                return true;
            }

            return false;
        }

        enum ConfirmationType
        {
            Confirmed,
            Unconfirmed
        }
    }
}
