using System;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class AddressWatch : RuledWatch<AddressRule>
    {
        public AddressWatch(AddressRule rule, uint256 startBlock, AddressWatchType type)
            : base(rule, startBlock)
        {
            Type = type;
        }

        public AddressWatch(AddressRule rule, uint256 startBlock, AddressWatchType type, DateTime startTime)
            : base(rule, startBlock, startTime)
        {
            Type = type;
        }

        public AddressWatch(AddressRule rule, uint256 startBlock, AddressWatchType type, DateTime startTime, Guid id)
            : base(rule, startBlock, startTime, id)
        {
            Type = type;
        }

        public AddressWatchType Type { get; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            return ((AddressWatch)obj).Type == Type;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
