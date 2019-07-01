using System;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class AddressRule : ExpirableRule
    {
        public AddressRule(BitcoinAddress address, BalanceChangeType balanceChangeType)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Address = address;
            BalanceChangeType = balanceChangeType;
        }

        public AddressRule(BitcoinAddress address, BalanceChangeType balanceChangeType, Guid id) : base(id)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            Address = address;
            BalanceChangeType = balanceChangeType;
        }

        public BitcoinAddress Address { get; }

        public BalanceChangeType BalanceChangeType { get; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            var other = (AddressRule)obj;

            return other.Address.Equals(Address) && other.BalanceChangeType == BalanceChangeType;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
