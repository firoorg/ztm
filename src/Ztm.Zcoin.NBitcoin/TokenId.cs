using System;
using System.ComponentModel;

namespace Ztm.Zcoin.NBitcoin
{
    [TypeConverter(typeof(TokenIdConverter))]
    public struct TokenId
    {
        readonly uint value;

        public TokenId(long value)
        {
            if (value <= 0 || value > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            this.value = (uint)value;
        }

        public bool IsValid => this.value != 0;

        public long Value => IsValid ? this.value : throw new InvalidOperationException("The identifier is not valid.");

        public static TokenId Parse(string s)
        {
            try
            {
                return new TokenId(long.Parse(s));
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is OverflowException)
            {
                throw new FormatException("The value is not valid.", ex);
            }
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return "";
            }

            return this.value.ToString();
        }

        public static implicit operator TokenId(long value)
        {
            return new TokenId(value);
        }
    }
}
