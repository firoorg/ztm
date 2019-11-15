using System;
using System.ComponentModel;
using System.Globalization;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public sealed class PropertyAmountConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) ||
                sourceType == typeof(int) ||
                sourceType == typeof(long) ||
                sourceType == typeof(decimal))
            {
                return true;
            }

            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(long) || destinationType == typeof(decimal))
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
                    return PropertyAmount.Parse(s);
                }
                catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                {
                    throw new NotSupportedException($"Cannot convert {s} to {typeof(PropertyAmount)}.", ex);
                }
            }
            else if (value is long n)
            {
                return new PropertyAmount(n);
            }
            else if (value is decimal d)
            {
                try
                {
                    return PropertyAmount.FromDivisible(d);
                }
                catch (ArgumentException ex)
                {
                    throw new NotSupportedException($"Cannot convert {d} to {typeof(PropertyAmount)}.", ex);
                }
            }
            else
            {
                throw new NotSupportedException(
                    $"Don't know how to convert {value.GetType()} to {typeof(PropertyAmount)}."
                );
            }
        }

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            var amount = (PropertyAmount)value;

            if (destinationType == typeof(long))
            {
                return amount.Indivisible;
            }
            else if (destinationType == typeof(decimal))
            {
                return amount.Divisible;
            }
            else
            {
                throw new NotSupportedException($"Don't know how to convert {value.GetType()} to {destinationType}.");
            }
        }
    }
}
