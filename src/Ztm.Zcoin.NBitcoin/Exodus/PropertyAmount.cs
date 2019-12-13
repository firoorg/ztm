using System;
using System.ComponentModel;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    [TypeConverter(typeof(PropertyAmountConverter))]
    public struct PropertyAmount : IEquatable<PropertyAmount>
    {
        public static readonly PropertyAmount MinusOne = new PropertyAmount(-1);
        public static readonly PropertyAmount One = new PropertyAmount(1);
        public static readonly PropertyAmount Zero = new PropertyAmount(0);

        readonly long value;

        public PropertyAmount(long value)
        {
            this.value = value;
        }

        public decimal Divisible => this.value / 100000000m;

        public long Indivisible => this.value;

        public static PropertyAmount FromDivisible(decimal value)
        {
            if (value % 0.00000001m != 0m)
            {
                throw new ArgumentException("The value has too much precision.", nameof(value));
            }

            value *= 100000000m;

            if (value < long.MinValue || value > long.MaxValue)
            {
                throw new ArgumentException("The value is not valid.", nameof(value));
            }

            return new PropertyAmount((long)value);
        }

        /// <summary>
        /// Convert a string that represents property amount.
        /// </summary>
        /// <param name="s">
        /// The string to convert. The value will be treated as divisible if it contains a period; otherwise it will be
        /// treated as indivisible.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="s"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// <paramref name="s"/> is not in the correct format.
        /// </exception>
        /// <exception cref="OverflowException">
        /// <paramref name="s"/> is less than minimum or greater than maximum value.
        /// </exception>
        public static PropertyAmount Parse(string s)
        {
            // Convert string to decimal first. We use decimal to be able to support both divisible and indivisible.
            var value = decimal.Parse(s);

            // Determine type. We need to check if there is a period in the string instead of modulo due to '1.0' will
            // be treated as indivisible.
            if (s.IndexOf('.') != -1)
            {
                if (value % 0.00000001m != 0m)
                {
                    throw new FormatException("Too much precision.");
                }

                value *= 100000000m;
            }

            if (value < long.MinValue || value > long.MaxValue)
            {
                throw new OverflowException("The value is not in the valid range.");
            }

            return new PropertyAmount((long)value);
        }

        public bool Equals(PropertyAmount other)
        {
            return this.value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((PropertyAmount)obj);
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public string ToString(PropertyType type)
        {
            // Don't override Object.ToString() due to we want it to return invalid amount.
            switch (type)
            {
                case PropertyType.Divisible:
                    return (this.value / 100000000m).ToString("0.00000000");
                case PropertyType.Indivisible:
                    return this.value.ToString();
                default:
                    throw new ArgumentException("The value is not valid.", nameof(type));
            }
        }

        public static PropertyAmount operator-(PropertyAmount amount)
        {
            return new PropertyAmount(checked(-amount.value));
        }

        public static bool operator<(PropertyAmount first, PropertyAmount second)
        {
            return first.value < second.value;
        }

        public static bool operator>(PropertyAmount first, PropertyAmount second)
        {
            return first.value > second.value;
        }

        public static bool operator<=(PropertyAmount first, PropertyAmount second)
        {
            return first.value <= second.value;
        }

        public static bool operator>=(PropertyAmount first, PropertyAmount second)
        {
            return first.value >= second.value;
        }

        public static PropertyAmount Negate(PropertyAmount amount)
        {
            return -amount;
        }
    }
}
