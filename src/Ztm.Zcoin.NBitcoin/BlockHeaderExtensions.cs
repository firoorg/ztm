using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public static class BlockHeaderExtensions
    {
        public static bool IsMtp(this BlockHeader header)
        {
            return ((ZcoinBlockHeader)header).IsMtp;
        }

        public static void SetMtpHashData(this BlockHeader header, MTPHashData data)
        {
            ((ZcoinBlockHeader)header).MtpHashData = data;
        }

        public static MTPHashData GetMtpHashData(this BlockHeader header)
        {
            return ((ZcoinBlockHeader)header).MtpHashData;
        }

        public static void SetMtpHashValue(this BlockHeader header, uint256 value)
        {
            ((ZcoinBlockHeader)header).MtpHashValue = value;
        }

        public static uint256 GetMtpHashValue(this BlockHeader header)
        {
            return ((ZcoinBlockHeader)header).MtpHashValue;
        }

        public static void SetMtpVersion(this BlockHeader header, int version)
        {
            ((ZcoinBlockHeader)header).MtpVersion = version;
        }

        public static int GetMtpVersion(this BlockHeader header)
        {
            return ((ZcoinBlockHeader)header).MtpVersion;
        }

        public static void SetReserved1(this BlockHeader header, uint256 reserved)
        {
            ((ZcoinBlockHeader)header).Reserved1 = reserved;
        }

        public static uint256 GetReserved1(this BlockHeader header)
        {
            return ((ZcoinBlockHeader)header).Reserved1;
        }

        public static void SetReserved2(this BlockHeader header, uint256 reserved)
        {
            ((ZcoinBlockHeader)header).Reserved2 = reserved;
        }

        public static uint256 GetReserved2(this BlockHeader header)
        {
            return ((ZcoinBlockHeader)header).Reserved2;
        }
    }
}