using System;
using NBitcoin;
using Newtonsoft.Json;

namespace Ztm.WebApi.Converters
{
    public sealed class BitcoinAddressConverter : Converter<BitcoinAddress>
    {
        readonly Network network;

        public BitcoinAddressConverter(Network network)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            this.network = network;
        }

        public override BitcoinAddress ReadJson(
            JsonReader reader,
            Type objectType,
            BitcoinAddress existingValue,
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
                        $"Unexpected token {reader.TokenType} when parsing {typeof(BitcoinAddress)}.");
            }
        }

        public override void WriteJson(JsonWriter writer, BitcoinAddress value, JsonSerializer serializer)
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

        protected override BitcoinAddress Parse(string s)
        {
            return BitcoinAddress.Create(s, this.network);
        }
    }
}
