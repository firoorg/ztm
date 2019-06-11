using System;

namespace Ztm.Zcoin.NBitcoin
{
    public struct TokenAmount
    {
        readonly decimal value; // Positive represent divisible, negative represents indivisible.

        private TokenAmount(decimal value)
        {
            this.value = value;
        }

        public bool IsDivisible => this.value > 0;

        public bool IsIndivisible => this.value < 0;

        public bool IsValid => this.value != 0;

        public decimal Value => !IsValid
            ? throw new InvalidOperationException("The amount is not valid.")
            : IsDivisible ? this.value : -this.value;

        public static TokenAmount Divisible(decimal value)
        {
            if (value <= 0 || value > 92233720368.54775807m || (value % 0.00000001m) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            return new TokenAmount(value);
        }

        public static TokenAmount Indivisible(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            return new TokenAmount(-value);
        }

        public static TokenAmount Satoshi(long value, bool divisible)
        {
            if (value <= 0 || (!divisible && (value % 100000000) != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            var amount = (decimal)value / 100000000;

            return new TokenAmount(divisible ? amount : -amount);
        }

        public override string ToString()
        {
            return IsValid ? Value.ToString() : "";
        }
    }
}
