using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public sealed class BalanceChange : IEquatable<BalanceChange>
    {
        public BalanceChange(
            BitcoinAddress address,
            PropertyAmount amount,
            PropertyId property)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            this.Address = address;
            this.Amount = amount;
            this.Property = property;
        }

        public BitcoinAddress Address { get; }
        public PropertyAmount Amount { get; }
        public PropertyId Property { get; }

        public bool Equals(BalanceChange other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Address.Equals(other.Address)
                && this.Amount.Equals(other.Amount)
                && this.Property.Equals(other.Property);
        }
    }
}