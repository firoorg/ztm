using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionRetrievers
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

        public override bool Equals(object other)
        {
            if (other == null || other.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((BalanceChange)other);
        }

        public override int GetHashCode()
        {
            int h = 0;

            h ^= this.Address.GetHashCode();
            h ^= this.Amount.GetHashCode();
            h ^= this.Property.GetHashCode();

            return h;
        }
    }
}