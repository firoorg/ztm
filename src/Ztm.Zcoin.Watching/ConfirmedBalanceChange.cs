using System;

namespace Ztm.Zcoin.Watching
{
    public sealed class ConfirmedBalanceChange<TContext, TAmount> : BalanceChange<TContext, TAmount>
    {
        public ConfirmedBalanceChange(TContext context, TAmount amount, int confirmation) : base(context, amount)
        {
            if (confirmation < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(confirmation),
                    confirmation,
                    "The value is not a valid confirmation."
                );
            }

            Confirmation = confirmation;
        }

        public int Confirmation { get; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj) && Confirmation == ((ConfirmedBalanceChange<TContext, TAmount>)obj).Confirmation;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Confirmation.GetHashCode();
        }
    }
}
