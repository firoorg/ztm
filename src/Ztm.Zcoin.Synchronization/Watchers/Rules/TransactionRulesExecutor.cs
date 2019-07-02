using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class TransactionRulesExecutor : ExpirableRulesExecutor<TransactionRule, TransactionWatch>
    {
        readonly ITransactionRulesStorage storage;

        public TransactionRulesExecutor(
            ITransactionRulesStorage storage,
            IRulesExpireWatcher<TransactionRule, TransactionWatch> expireWatcher) : base(storage, expireWatcher)
        {
            this.storage = storage;
        }

        protected override Task<bool> DisassociateRuleAsyc(
            TransactionWatch watch,
            WatchRemoveReason reason,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(reason.HasFlag(WatchRemoveReason.Completed));
        }

        protected override async Task<IEnumerable<TransactionWatch>> ExecuteRulesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken)
        {
            var rules = await this.storage.GetRulesByTransactionHashesAsync(
                block.Transactions.Select(t => t.GetHash()).ToArray(),
                cancellationToken
            );

            block.Header.PrecomputeHash(invalidateExisting: false, lazily: false);

            return rules.Select(r => new TransactionWatch(r, block.GetHash())).ToArray();
        }
    }
}
