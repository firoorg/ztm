using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public sealed class TransactionEncoder : ITransactionEncoder
    {
        public const int MinSize = 4;

        readonly Dictionary<int, ITransactionPayloadEncoder> encoders;

        public TransactionEncoder(IEnumerable<ITransactionPayloadEncoder> encoders)
        {
            if (encoders == null)
            {
                throw new ArgumentNullException(nameof(encoders));
            }

            this.encoders = encoders.ToDictionary(e => e.Type);
        }

        public ExodusTransaction Decode(BitcoinAddress sender, BitcoinAddress receiver, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using (var stream = new MemoryStream(data, false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                int version, type;

                // We need to do an ugly cast due to we want to preserve bits.
                // - "(short)ReadUInt16()" will give the same bits but "short" type (e.g. 65535 will become -1).
                // - "(ushort)IPAddress.NetworkToHostOrder()" will give 65535 instead of -1 when assign to "int".
                try
                {
                    version = (ushort)IPAddress.NetworkToHostOrder((short)reader.ReadUInt16());
                    type = (ushort)IPAddress.NetworkToHostOrder((short)reader.ReadUInt16());
                }
                catch (EndOfStreamException ex)
                {
                    throw new TransactionTooShortException(MinSize, ex);
                }

                if (!this.encoders.TryGetValue(type, out var encoder))
                {
                    throw new TransactionFieldException(
                        TransactionFieldException.TypeField,
                        "The value is unknown transaction type."
                    );
                }

                return encoder.Decode(sender, receiver, reader, version);
            }
        }
    }
}
