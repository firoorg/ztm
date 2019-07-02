using System;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class TransactionWatch : RuledWatch<TransactionRule>
    {
        public TransactionWatch(TransactionRule rule, uint256 startBlock)
            : base(rule, startBlock)
        {
        }

        public TransactionWatch(TransactionRule rule, uint256 startBlock, DateTime startTime)
            : base(rule, startBlock, startTime)
        {
        }

        public TransactionWatch(TransactionRule rule, uint256 startBlock, DateTime startTime, Guid id)
            : base(rule, startBlock, startTime, id)
        {
        }
    }
}
