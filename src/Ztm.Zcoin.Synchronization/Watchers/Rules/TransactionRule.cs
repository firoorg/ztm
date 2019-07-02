using System;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class TransactionRule : ExpirableRule
    {
        public TransactionRule(uint256 transactionHash)
        {
            if (transactionHash == null)
            {
                throw new ArgumentNullException(nameof(transactionHash));
            }

            TransactionHash = transactionHash;
        }

        public TransactionRule(uint256 transactionHash, Guid id) : base(id)
        {
            if (transactionHash == null)
            {
                throw new ArgumentNullException(nameof(transactionHash));
            }

            TransactionHash = transactionHash;
        }

        public uint256 TransactionHash { get; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            return ((TransactionRule)obj).TransactionHash == TransactionHash;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
