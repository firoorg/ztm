using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NBitcoin;

namespace Ztm.Data.Entity
{
    public static class Converters
    {
        public static readonly ValueConverter<uint256, byte[]> UInt256ToBytesConverter = new ValueConverter<uint256, byte[]>(
            v => v.ToBytes(true),
            v => new uint256(v),
            new ConverterMappingHints(size: 32)
        );

        public static readonly ValueConverter<Uri, string> UriToStringConverter = new ValueConverter<Uri, string>(
            v => v.ToString(),
            v => new Uri(v)
        );
    }
}
