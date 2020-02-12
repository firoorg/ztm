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

            if (objectType != typeof(BitcoinAddress))
            {
                throw new ArgumentException("The value is not supported.", nameof(objectType));
            }

            switch (reader.TokenType)
            {
                case JsonToken.Null:
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
