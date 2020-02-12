using System;
using NBitcoin;
using Newtonsoft.Json;

namespace Ztm.WebApi.Converters
{
    public sealed class MoneyConverter : Converter<Money>
    {
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

            if (objectType != typeof(Money))
            {
                throw new ArgumentException("The value is not supported.", nameof(objectType));
            }

            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.String:
                    return Money.Parse(reader.Value.ToString());
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

            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.ToString());
            }
        }

        protected override Money Parse(string s)
        {
            return Money.Parse(s);
        }
    }
}
