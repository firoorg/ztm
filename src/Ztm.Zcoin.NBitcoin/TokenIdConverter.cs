using System;
using System.ComponentModel;
using System.Globalization;

namespace Ztm.Zcoin.NBitcoin
{
    public sealed class TokenIdConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) ||
                sourceType == typeof(int) ||
                sourceType == typeof(long))
            {
                return true;
            }

            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string) ||
                destinationType == typeof(uint) ||
                destinationType == typeof(long))
            {
                return true;
            }

            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Normalize value.
            switch (value)
            {
                case int i:
                    value = (long)i;
                    break;
            }

            // Convert.
            if (value is string s)
            {
                try
                {
                    return TokenId.Parse(s);
                }
                catch (FormatException ex)
                {
                    throw new NotSupportedException($"Cannot convert {s} to {typeof(TokenId)}.", ex);
                }
            }
            else if (value is long n)
            {
                try
                {
                    return new TokenId(n);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new NotSupportedException($"Cannot convert {n} to {typeof(TokenId)}.", ex);
                }
            }
            else
            {
                throw new NotSupportedException($"Don't know how to convert {value.GetType()} to {typeof(TokenId)}.");
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            var id = (TokenId)value;

            if (!id.IsValid)
            {
                throw new NotSupportedException($"Cannot convert invalid {typeof(TokenId)}.");
            }

            if (destinationType == typeof(string))
            {
                return id.ToString();
            }
            else if (destinationType == typeof(uint))
            {
                return (uint)id.Value;
            }
            else if (destinationType == typeof(long))
            {
                return id.Value;
            }
            else
            {
                throw new NotSupportedException($"Don't know how to convert {typeof(TokenId)} to {destinationType}.");
            }
        }
    }
}
