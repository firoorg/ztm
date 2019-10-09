using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public sealed class MTPHashData : IBitcoinSerializable
    {
        const int MTP_L = 64;

        byte[] hashRootMTP;
        byte[] nBlockMTP;
        readonly Collection<byte[]>[] nProofMTP;

        public MTPHashData()
        {
            hashRootMTP = new byte[16];
            nBlockMTP = new byte[MTP_L*2*128*8];
            nProofMTP = new Collection<byte[]>[MTP_L*3];
        }

        public byte[] BlockMTP
        {
            get { return this.nBlockMTP; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Length != this.nBlockMTP.Length)
                {
                    throw new ArgumentException("Value is not valid.", nameof(value));
                }

                this.nBlockMTP = value;
            }
        }

        public byte[] HashRootMTP
        {
            get { return this.hashRootMTP; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Length != this.hashRootMTP.Length)
                {
                    throw new ArgumentException("Value is not valid.", nameof(value));
                }

                this.hashRootMTP = value;
            }
        }

        public IReadOnlyList<IList<byte[]>> ProofMTP
        {
            get { return this.nProofMTP; }
        }

        public void ReadWrite(BitcoinStream stream)
        {
            if (stream.Serializing)
            {
                // Write.
                stream.Inner.Write(hashRootMTP, 0, hashRootMTP.Length);
                stream.Counter.AddWritten(hashRootMTP.Length);

                stream.Inner.Write(nBlockMTP, 0, nBlockMTP.Length);
                stream.Counter.AddWritten(nBlockMTP.Length);

                for (var i = 0; i < MTP_L*3; i++)
                {
                    Debug.Assert(nProofMTP[i].Count < 256);

                    stream.ReadWrite((byte)nProofMTP[i].Count);

                    foreach (var mtpData in nProofMTP[i])
                    {
                        Debug.Assert(mtpData.Length == 16);

                        stream.Inner.Write(mtpData, 0, mtpData.Length);
                        stream.Counter.AddWritten(mtpData.Length);
                    }
                }
            }
            else
            {
                // Read.
                hashRootMTP = stream.Inner.ReadBytes(hashRootMTP.Length);
                stream.Counter.AddReaden(hashRootMTP.Length);

                nBlockMTP = stream.Inner.ReadBytes(nBlockMTP.Length);
                stream.Counter.AddReaden(nBlockMTP.Length);

                for (var i = 0; i < MTP_L*3; i++)
                {
                    byte numberOfProofBlocks = 0;

                    stream.ReadWrite(ref numberOfProofBlocks);

                    nProofMTP[i] = new Collection<byte[]>();

                    for (byte j = 0; j < numberOfProofBlocks; j++)
                    {
                        var mtpData = stream.Inner.ReadBytes(16);
                        stream.Counter.AddReaden(mtpData.Length);

                        nProofMTP[i].Add(mtpData);
                    }
                }
            }
        }
    }
}
