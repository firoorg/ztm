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
                sourceType == typeof(float) ||
                sourceType == typeof(double) ||
                sourceType == typeof(decimal))
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
                case float f:
                    value = (decimal)f;
                    break;
                case double d:
                    value = (decimal)d;
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
                try
                {
                    return PropertyAmount.Indivisible(n);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new NotSupportedException($"Cannot convert {n} to {typeof(PropertyAmount)}.", ex);
                }
            }
            else if (value is decimal d)
            {
                try
                {
                    return PropertyAmount.Divisible(d);
                }
                catch (ArgumentOutOfRangeException ex)
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

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (!((PropertyAmount)value).IsValid)
            {
                throw new NotSupportedException("The value is not valid.");
            }

            // We supported only string.
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[]
            {
                PropertyAmount.MinDivisible,
                PropertyAmount.MinIndivisible,
                PropertyAmount.MaxDivisible,
                PropertyAmount.MaxIndivisible
            });
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}
