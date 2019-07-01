using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Ztm.Configuration;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class AddressRulesExecutor : ExpirableRulesExecutor<AddressRule, AddressWatch>
    {
        readonly IAddressRulesStorage storage;
        readonly IBlocksStorage blocks;
        readonly Network network;

        public AddressRulesExecutor(
            IConfiguration config,
            IAddressRulesStorage storage,
            IRulesExpireWatcher<AddressRule, AddressWatch> expireWatcher,
            IBlocksStorage blocks) : base(storage, expireWatcher)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            this.storage = storage;
            this.blocks = blocks;
            this.network = ZcoinNetworks.Instance.GetNetwork(config.GetZcoinSection().Network.Type);
        }

        protected override Task<bool> DisassociateRuleAsyc(
            AddressWatch watch,
            WatchRemoveReason reason,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(reason.HasFlag(WatchRemoveReason.Completed));
        }

        protected override async Task<IEnumerable<AddressWatch>> ExecuteRulesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken)
        {
            block.Header.PrecomputeHash(invalidateExisting: false, lazily: false);

            // Get all involved addresses in a block.
            var debits = await GetDebitsAsync(block, cancellationToken);
            var credits = GetCredits(block);
            var addresses = debits.Concat(credits).Select(e => e.Key).Distinct().ToArray();

            // Load rules for all addresses.
            var rules = await this.storage.GetRulesByAddressesAsync(addresses, cancellationToken);

            // Filter rules based on interested balance changes type.
            var watches = new Collection<AddressWatch>();

            foreach (var rule in rules)
            {
                if (rule.BalanceChangeType.HasFlag(BalanceChangeType.Debit) && debits.ContainsKey(rule.Address))
                {
                    watches.Add(new AddressWatch(rule, block.GetHash(), AddressWatchType.Debit));
                }

                if (rule.BalanceChangeType.HasFlag(BalanceChangeType.Credit) && credits.ContainsKey(rule.Address))
                {
                    watches.Add(new AddressWatch(rule, block.GetHash(), AddressWatchType.Credit));
                }
            }

            return watches;
        }

        protected override Task OnRuleExpiredAsync(AddressRule rule, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        IReadOnlyDictionary<BitcoinAddress, Money> GetCredits(ZcoinBlock block)
        {
            var balances = new Dictionary<BitcoinAddress, Money>();

            foreach (var tx in block.Transactions)
            foreach (var output in tx.Outputs)
            {
                var address = output.ScriptPubKey.GetDestinationAddress(this.network);

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

        async Task<IReadOnlyDictionary<BitcoinAddress, Money>> GetDebitsAsync(
            ZcoinBlock block,
            CancellationToken cancellationToken)
        {
            var balances = new Dictionary<BitcoinAddress, Money>();

            foreach (var tx in block.Transactions)
            for (int i = 0; i < tx.Inputs.Count; i++)
            {
                var input = tx.Inputs[i];

                if (input.PrevOut.Hash == uint256.Zero)
                {
                    // Coinbase or Zerocoin/Sigma spend.
                    continue;
                }

                // Load spend transaction.
                var spendTx = await this.blocks.GetTransactionAsync(input.PrevOut.Hash, cancellationToken);

                if (spendTx == null)
                {
                    throw new ArgumentException($"Unknown input {i} in transaction {tx.GetHash()}.", nameof(block));
                }
                else if (input.PrevOut.N >= spendTx.Outputs.Count)
                {
                    throw new ArgumentException(
                        $"Invalid spend index for input {i} in transaction {tx.GetHash()}.",
                        nameof(block)
                    );
                }

                // Extract owner address.
                var spend = spendTx.Outputs[input.PrevOut.N];
                var address = spend.ScriptPubKey.GetDestinationAddress(this.network);

                if (address == null)
                {
                    throw new ArgumentException(
                        $"Invalid spend index for input {i} in transaction {tx.GetHash()}.",
                        nameof(block)
                    );
                }

                // Add debit to address.
                balances[address] = balances.ContainsKey(address)
                    ? balances[address] + spend.Value
                    : spend.Value;
            }

            return balances;
        }
    }
}
