using System;
using NBitcoin;
using Newtonsoft.Json;

namespace Ztm.Zcoin.NBitcoin
{
    public sealed class BitcoinAddressJsonConverter : JsonConverter<BitcoinAddress>
    {
        readonly Network network;

        public BitcoinAddressJsonConverter(Network network)
        {
            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            this.network = network;
        }

        public override BitcoinAddress ReadJson(JsonReader reader, Type objectType, BitcoinAddress existingValue, bool hasExistingValue, JsonSerializer serializer)
        {

            return BitcoinAddress.Create((string)reader.Value, this.network);
        }

        public override void WriteJson(JsonWriter writer, BitcoinAddress value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}