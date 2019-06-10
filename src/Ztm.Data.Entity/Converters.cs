using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NBitcoin;

namespace Ztm.Data.Entity
{
    public static class Converters
    {
        public static readonly ValueConverter<IPAddress, string> IPAddressToStringConverter = new ValueConverter<IPAddress, string>(
            v => v.ToString(),
            v => IPAddress.Parse(v),
            new ConverterMappingHints(size: 45, unicode: false)
        );

        public static readonly ValueConverter<Script, byte[]> ScriptToBytesConverter = new ValueConverter<Script, byte[]>(
            v => v.ToBytes(true),
            v => Script.FromBytesUnsafe(v)
        );

        public static readonly ValueConverter<Target, long> TargetToInt64 = new ValueConverter<Target, long>(
            v => v.ToCompact(),
            v => new Target((uint)v)
        );

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
