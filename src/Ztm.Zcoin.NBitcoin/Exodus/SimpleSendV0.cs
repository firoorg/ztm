using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public class SimpleSendV0 : ExodusTransaction
    {
        public const int StaticId = 0;

        public SimpleSendV0(BitcoinAddress sender, BitcoinAddress receiver, PropertyId property, PropertyAmount amount)
            : base(sender, receiver)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (amount <= PropertyAmount.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "The value is less than one.");
            }

            Property = property;
            Amount = amount;
        }

        public PropertyAmount Amount { get; }

        public override int Id => StaticId;

        public PropertyId Property { get; }

        public override int Version => 0;
    }
}
