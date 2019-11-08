using System;
using System.ComponentModel;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    [TypeConverter(typeof(PropertyIdConverter))]
    public sealed class PropertyId
    {
        public const long MaxValue = uint.MaxValue;
        public const long MinValue = 1;

        readonly uint value;

        public PropertyId(long value)
        {
            if (value < MinValue || value > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The value is not valid.");
            }

            this.value = (uint)value;
        }

        public long Value => this.value;

        public static PropertyId Parse(string s)
        {
            try
            {
                return new PropertyId(long.Parse(s));
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is OverflowException)
            {
                throw new FormatException("The value is not valid.", ex);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return ((PropertyId)obj).value == this.value;
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public override string ToString()
        {
            return this.value.ToString();
        }

        public static bool operator==(PropertyId first, PropertyId second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (ReferenceEquals(first, null)) // Don't use == here due to it will recursive.
            {
                return false;
            }

            return first.Equals(second);
        }

        public static bool operator!=(PropertyId first, PropertyId second)
        {
            return !(first == second);
        }
    }
}
