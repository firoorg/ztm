using System;
using System.ComponentModel;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    [TypeConverter(typeof(PropertyAmountConverter))]
    public struct PropertyAmount
    {
        public static readonly PropertyAmount MaxDivisible = PropertyAmount.Divisible(92233720368.54775807m);

        public static readonly PropertyAmount MaxIndivisible = PropertyAmount.Indivisible(long.MaxValue);

        public static readonly PropertyAmount MinDivisible = PropertyAmount.Divisible(0.00000001m);

        public static readonly PropertyAmount MinIndivisible = PropertyAmount.Indivisible(1);

        readonly long value;
        readonly PropertyType type;

        private PropertyAmount(long value, PropertyType type)
        {
            this.value = value;
            this.type = type;
        }

        public bool IsValid => this.value > 0;

        public PropertyType Type => IsValid
            ? this.type
            : throw new InvalidOperationException("The amount is not valid.");

        public static PropertyAmount Divisible(decimal value)
        {
            if (value <= 0m || value > 92233720368.54775807m || (value % 0.00000001m) != 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            value *= 100000000;

            return new PropertyAmount((long)value, PropertyType.Divisible);
        }

        public static PropertyAmount Indivisible(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            return new PropertyAmount(value, PropertyType.Indivisible);
        }

        public static PropertyAmount Parse(string s)
        {
            // Convert string to decimal first.
            var amount = decimal.Parse(s);

            if (amount < 0)
            {
                throw new FormatException("The string is not valid.");
            }
            else if (amount == 0)
            {
                return default(PropertyAmount);
            }

            // Determine type. We need to check if there is a period in the string instead of modulo due to '1.0' will
            // be treated as indivisible.
            var type = (s.IndexOf('.') != -1) ? PropertyType.Divisible : PropertyType.Indivisible;

            if (type == PropertyType.Divisible)
            {
                if (amount > 92233720368.54775807m || (amount % 0.00000001m) != 0m)
                {
                    throw new FormatException("The string is not valid.");
                }

                amount *= 100000000;
            }
            else if (amount > long.MaxValue)
            {
                throw new FormatException("The string is not valid.");
            }

            return new PropertyAmount((long)amount, type);
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return "";
            }

            switch (this.type)
            {
                case PropertyType.Divisible:
                    return ((decimal)this.value / 100000000).ToString("0.00000000");
                case PropertyType.Indivisible:
                    return this.value.ToString();
                default:
                    throw new InvalidOperationException($"Property type {this.type} is not valid.");
            }
        }
    }
}
