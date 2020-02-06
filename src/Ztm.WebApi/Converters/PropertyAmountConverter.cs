using System;
using Newtonsoft.Json;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Converters
{
    public sealed class PropertyAmountConverter : Converter<PropertyAmount>
    {
        readonly PropertyType type;

        public PropertyAmountConverter(PropertyType type)
        {
            this.type = type;
        }

        public override PropertyAmount ReadJson(
            JsonReader reader,
            Type objectType,
            PropertyAmount existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"Unexpected token {reader.TokenType} when parsing {typeof(PropertyAmount)}.");
            }

            return Parse(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, PropertyAmount value, JsonSerializer serializer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteValue(value.ToString(this.type));
        }

        protected override PropertyAmount Parse(string s)
        {
            // We cannot use TokenAmount.Parse because we want to allow user to specify integer for divisible.
            switch (this.type)
            {
                case PropertyType.Divisible:
                    return PropertyAmount.FromDivisible(decimal.Parse(s));
                case PropertyType.Indivisible:
                    return new PropertyAmount(long.Parse(s));
                default:
                    throw new InvalidOperationException("The configuration for converter is not valid.");
            }
        }
    }
}
