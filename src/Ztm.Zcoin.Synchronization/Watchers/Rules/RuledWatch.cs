using System;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class RuledWatch<T> : Watch where T : Rule
    {
        public RuledWatch(T rule, uint256 startBlock) : base(startBlock)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            Rule = rule;
        }

        public RuledWatch(T rule, uint256 startBlock, DateTime startTime) : base(startBlock, startTime)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            Rule = rule;
        }

        public RuledWatch(T rule, uint256 startBlock, DateTime startTime, Guid id) : base(startBlock, startTime, id)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            Rule = rule;
        }

        public T Rule { get; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            return ((RuledWatch<T>)obj).Rule.Equals(Rule);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
