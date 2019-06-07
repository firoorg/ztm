using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Ztm.Configuration;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Contexts.Main;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    [Guid("9b790cf5-53f3-4cce-a1bb-a39ad0ab6c31")]
    public class AddressWatcher : ITransactionConfirmationListener
    {
        readonly IMainDatabaseFactory db;
        readonly IBlocksStorage blocks;
        readonly Dictionary<Guid, IAddressListener> listeners;
        readonly Network zcoinNetwork;

        public AddressWatcher(
            IConfiguration config,
            IMainDatabaseFactory db,
            IBlocksStorage blocks,
            IEnumerable<IAddressListener> listeners)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

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
            this.zcoinNetwork = ZcoinNetworks.Instance.GetNetwork(config.GetZcoinSection().Network.Type);
        }

        async Task<(int total, int removed)> InvokeListenersAsync(
            IReadOnlyDictionary<BitcoinAddress, Money> amounts,
            AddressWatchingType watchingType,
            int confirmation,
            ConfirmationType confirmationType)
        {
            // Load watches.
            var addresses = amounts.Keys.Select(a => a.ToString()).ToArray();
            WatchingAddress[] watches;

            using (var db = this.db.CreateDbContext())
            {
                watches = await db.WatchingAddresses
                    .Where(w => addresses.Contains(w.Address) && w.Type == watchingType)
                    .ToArrayAsync(CancellationToken.None);
            }

            if (watches.Length == 0)
            {
                return (total: 0, removed: 0);
            }

            // Invoke listeners.
            var watchesToRemove = new Collection<WatchingAddress>();

            foreach (var watch in watches)
            {
                var listener = this.listeners[watch.Listener];
                var address = BitcoinAddress.Create(watch.Address, this.zcoinNetwork);
                var amount = amounts[address];
                bool keep;

                switch (watchingType)
                {
                    case AddressWatchingType.Credit:
                        keep = await listener.CreditConfirmAsync(address, amount, confirmationType, confirmation);
                        break;
                    case AddressWatchingType.Debit:
                        keep = await listener.DebitConfirmAsync(address, amount, confirmationType, confirmation);
                        break;
                    default:
                        throw new ArgumentException($"Value is not valid.", nameof(watchingType));
                }

                if (!keep)
                {
                    watchesToRemove.Add(watch);
                }
                else if (confirmationType == ConfirmationType.Unconfirming && confirmation == 1)
                {
                    throw new InvalidOperationException($"{listener.GetType()} want to continue watching address {address} but it transaction going to be removed now.");
                }
            }

            // Remove watches.
            if (watchesToRemove.Count > 0)
            {
                using (var db = this.db.CreateDbContext())
                {
                    db.WatchingAddresses.RemoveRange(watchesToRemove);
                    await db.SaveChangesAsync(CancellationToken.None);
                }
            }

            return (total: watches.Length, removed: watchesToRemove.Count);
        }

        async Task<IReadOnlyDictionary<BitcoinAddress, Money>> GetTransactionDebitsAsync(ZcoinTransaction transaction)
        {
            var balances = new Dictionary<BitcoinAddress, Money>();

            for (var i = 0; i < transaction.Inputs.Count; i++)
            {
                var input = transaction.Inputs[i];

                if (input.PrevOut.Hash == uint256.Zero)
                {
                    // Coinbase or Zerocoin/Sigma spend.
                    continue;
                }

                // Load output's transaction.
                var spendTx = await this.blocks.GetTransactionAsync(input.PrevOut.Hash, CancellationToken.None);

                if (spendTx == null)
                {
                    throw new ArgumentException($"Unknown input at index {i}.", nameof(transaction));
                }
                else if (input.PrevOut.N >= spendTx.Outputs.Count)
                {
                    throw new ArgumentException($"Invalid spend index for input {i}.", nameof(transaction));
                }

                // Extract owner address.
                var spend = spendTx.Outputs[input.PrevOut.N];
                var address = spend.ScriptPubKey.GetDestinationAddress(this.zcoinNetwork);

                if (address == null)
                {
                    throw new ArgumentException($"Invalid spend index for input {i}.", nameof(transaction));
                }

                // Add debit to address.
                balances[address] = balances.ContainsKey(address)
                    ? balances[address] + spend.Value
                    : spend.Value;
            }

            return balances;
        }

        IReadOnlyDictionary<BitcoinAddress, Money> GetTransactionCredits(ZcoinTransaction transaction)
        {
            var balances = new Dictionary<BitcoinAddress, Money>();

            foreach (var output in transaction.Outputs)
            {
                var address = output.ScriptPubKey.GetDestinationAddress(this.zcoinNetwork);

                if (address == null)
                {
                    // Not pay to address script.
                    continue;
                }

                balances[address] = balances.ContainsKey(address)
                    ? balances[address] + output.Value
                    : output.Value;
            }

            return balances;
        }

        Guid ITransactionConfirmationListener.Id => GetType().GUID;

        async Task<bool> ITransactionConfirmationListener.StartListenAsync(ZcoinTransaction transaction)
        {
            // Get all involved address in transaction.
            var debits = await GetTransactionDebitsAsync(transaction);
            var credits = GetTransactionCredits(transaction);

            // Determine new watches.
            var watches = new Collection<WatchingAddress>();

            foreach (var address in debits.Concat(credits).Select(p => p.Key).Distinct())
            foreach (var listener in this.listeners)
            {
                var enable = await listener.Value.StartListenAsync(address);

                if (enable.HasFlag(AddressListeningType.Credit) && credits.ContainsKey(address))
                {
                    watches.Add(new WatchingAddress()
                    {
                        Address = address.ToString(),
                        Type = AddressWatchingType.Credit,
                        Listener = listener.Key,
                        StartTime = DateTime.UtcNow
                    });
                }

                if (enable.HasFlag(AddressListeningType.Debit) && debits.ContainsKey(address))
                {
                    watches.Add(new WatchingAddress()
                    {
                        Address = address.ToString(),
                        Type = AddressWatchingType.Debit,
                        Listener = listener.Key,
                        StartTime = DateTime.UtcNow
                    });
                }
            }

            // Add new watches.
            if (watches.Count > 0)
            {
                using (var db = this.db.CreateDbContext())
                {
                    await db.WatchingAddresses.AddRangeAsync(watches, CancellationToken.None);
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                return true;
            }

            return false;
        }

        async Task<bool> ITransactionConfirmationListener.TransactionConfirmAsync(
            ZcoinTransaction transaction,
            ConfirmationType type,
            int confirmation)
        {
            // Extract all addresses from this transaction.
            var debits = await GetTransactionDebitsAsync(transaction);
            var credits = GetTransactionCredits(transaction);

            if (debits.Count == 0 && credits.Count == 0)
            {
                throw new ArgumentException("Transaction was not watched.", nameof(transaction));
            }

            // Invoke listeners.
            var watches = 0;
            var stopped = 0;

            if (debits.Count > 0)
            {
                var (total, removed) = await InvokeListenersAsync(
                    debits,
                    AddressWatchingType.Debit,
                    confirmation,
                    type
                );

                watches += total;
                stopped += removed;
            }

            if (credits.Count > 0)
            {
                var (total, removed) = await InvokeListenersAsync(
                    credits,
                    AddressWatchingType.Credit,
                    confirmation,
                    type
                );

                watches += total;
                stopped += removed;
            }

            if (watches == 0)
            {
                throw new ArgumentException("Transaction was not watched.", nameof(transaction));
            }

            return watches > stopped;
        }
    }
}
