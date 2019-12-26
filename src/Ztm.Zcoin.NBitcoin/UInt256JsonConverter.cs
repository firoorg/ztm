using System;
using NBitcoin;
using Newtonsoft.Json;

namespace Ztm.Zcoin.NBitcoin
{
    public sealed class UInt256JsonConverter : JsonConverter<uint256>
    {
        public override uint256 ReadJson(JsonReader reader, Type objectType, uint256 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return uint256.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, uint256 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}