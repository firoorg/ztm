using System;

namespace Ztm.Zcoin.Watching
{
    public class BalanceChange<TContext, TAmount>
    {
        public BalanceChange(TContext context, TAmount amount)
        {
            if (amount == null)
            {
                throw new ArgumentNullException(nameof(amount));
            }

            Context = context;
            Amount = amount;
        }

        public TAmount Amount { get; }

        public TContext Context { get; }

        public override bool Equals(object obj)
        {
            var other = obj as BalanceChange<TContext, TAmount>;

            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return Amount.Equals(other.Amount);
        }

        public override int GetHashCode()
        {
            return Amount.GetHashCode();
        }
    }
}
