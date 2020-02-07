using System;
using NBitcoin;
using Newtonsoft.Json;

namespace Ztm.WebApi.Converters
{
    public sealed class MoneyConverter : Converter<Money>
    {
        public override Money ReadJson(
            JsonReader reader,
            Type objectType,
            Money existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.String:
                    return Money.Parse(reader.Value.ToString());
                default:
                    throw new JsonSerializationException(
                        $"Unexpected token {reader.TokenType} when parsing {typeof(Money)}.");
            }
        }

        public override void WriteJson(JsonWriter writer, Money value, JsonSerializer serializer)
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
