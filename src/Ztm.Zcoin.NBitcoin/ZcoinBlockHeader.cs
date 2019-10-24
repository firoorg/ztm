using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class ZcoinBlockHeader : BlockHeader
    {
        static readonly DateTimeOffset GenesisBlockTime = DateTimeOffset.FromUnixTimeSeconds(1414776286);

        readonly DateTimeOffset mtpSwitchTime;
        int nVersionMTP;
        uint256 mtpHashValue;
        uint256 reserved1;
        uint256 reserved2;
        MTPHashData mtpHashData;

        #pragma warning disable CS0618
        public ZcoinBlockHeader(DateTimeOffset mtpSwitchTime)
        {
            this.mtpSwitchTime = mtpSwitchTime;
            this.nVersionMTP = 0x1000;
        }
        #pragma warning restore CS0618

        public bool IsMtp
        {
            get { return BlockTime > GenesisBlockTime && BlockTime >= mtpSwitchTime; }
        }

        public MTPHashData MtpHashData
        {
            get { return this.mtpHashData; }
            set { this.mtpHashData = value; }
        }

        public uint256 MtpHashValue
        {
            get { return this.mtpHashValue; }
            set { this.mtpHashValue = value; }
        }

        public int MtpVersion
        {
            get { return this.nVersionMTP; }
            set { this.nVersionMTP = value; }
        }

        public uint256 Reserved1
        {
            get { return this.reserved1; }
            set { this.reserved1 = value; }
        }

        public uint256 Reserved2
        {
            get { return this.reserved2; }
            set { this.reserved2 = value; }
        }

        public override void ReadWrite(BitcoinStream stream)
        {
            base.ReadWrite(stream);

            if (IsMtp)
            {
                stream.ReadWrite(ref nVersionMTP);
                stream.ReadWrite(ref mtpHashValue);
                stream.ReadWrite(ref reserved1);
                stream.ReadWrite(ref reserved2);

                if (stream.Serializing)
                {
                    // Write.
                    if (mtpHashData != null && stream.Type != SerializationType.Hash)
                    {
                        stream.ReadWrite(ref mtpHashData);
                    }
                }
                else
                {
                    // Read.
                    stream.ReadWrite(ref mtpHashData);
                }
            }
        }
    }
}
