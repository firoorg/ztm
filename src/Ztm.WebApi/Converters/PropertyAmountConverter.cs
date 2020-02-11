using System;
using Newtonsoft.Json;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Converters
{
    public sealed class PropertyAmountConverter : Converter<PropertyAmount, PropertyAmount?>
    {
        readonly PropertyType type;

        public PropertyAmountConverter(PropertyType type)
        {
            this.type = type;
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (objectType != typeof(PropertyAmount) && objectType != typeof(PropertyAmount?))
            {
                throw new ArgumentException("The value is not supported.", nameof(objectType));
            }

            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    if (objectType != typeof(PropertyAmount?))
                    {
                        goto default;
                    }

                    return null;
                case JsonToken.String:
                    return Parse(reader.Value.ToString());
                default:
                    throw new JsonSerializationException(
                        $"Unexpected token {reader.TokenType} when parsing {objectType}.");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            switch (value)
            {
                case PropertyAmount a:
                    writer.WriteValue(a.ToString(this.type));
                    break;
                case null:
                    writer.WriteNull();
                    break;
                default:
                    throw new ArgumentException(
                        $"The value with type {value.GetType()} is not supported.",
                        nameof(value));
            }
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
