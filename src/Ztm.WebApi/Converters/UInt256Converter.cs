using System;
using NBitcoin;
using Newtonsoft.Json;

namespace Ztm.WebApi.Converters
{
    public sealed class UInt256Converter : Converter<uint256>
    {
        public override uint256 ReadJson(
            JsonReader reader,
            Type objectType,
            uint256 existingValue,
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
                    return Parse(reader.Value.ToString());
                default:
                    throw new JsonSerializationException(
                        $"Unexpected token {reader.TokenType} when parsing {typeof(uint256)}.");
            }
        }

        public override void WriteJson(JsonWriter writer, uint256 value, JsonSerializer serializer)
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

        protected override uint256 Parse(string s)
        {
            return uint256.Parse(s);
        }
    }
}
