using System;
using System.ComponentModel;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    [TypeConverter(typeof(PropertyIdConverter))]
    public struct PropertyId
    {
        readonly uint value;

        public PropertyId(long value)
        {
            if (value <= 0 || value > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            this.value = (uint)value;
        }

        public bool IsValid => this.value != 0;

        public long Value => IsValid ? this.value : throw new InvalidOperationException("The identifier is not valid.");

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

        public override string ToString()
        {
            return IsValid ? this.value.ToString() : "";
        }

        public static implicit operator PropertyId(long value)
        {
            return new PropertyId(value);
        }
    }
}
