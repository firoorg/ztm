using System;
using System.IO;
using System.Net;
using System.Text;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public sealed class TransactionEncoder : ITransactionEncoder
    {
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
                    throw new TransactionTooShortException(4, ex);
                }

                return DecodeTransaction(type, version, sender, receiver, reader);
            }
        }

        ExodusTransaction DecodeTransaction(
            int type,
            int version,
            BitcoinAddress sender,
            BitcoinAddress receiver,
            BinaryReader reader)
        {
            switch (type)
            {
                case SimpleSendV0.StaticId:
                    return DecodeSimpleSend(version, sender, receiver, reader);
                default:
                    throw new TransactionFieldException(
                        TransactionFieldException.TypeField,
                        "The value is unknown transaction type."
                    );
            }
        }

        ExodusTransaction DecodeSimpleSend(
            int version,
            BitcoinAddress sender,
            BitcoinAddress receiver,
            BinaryReader reader)
        {
            PropertyId property;
            PropertyAmount amount;

            switch (version)
            {
                case 0:
                    // Parse.
                    try
                    {
                        property = DecodePropertyId(reader);
                        amount = DecodePropertyAmount(reader);
                    }
                    catch (EndOfStreamException ex)
                    {
                        throw new TransactionTooShortException(16, ex);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw new TransactionFieldException(
                            "property",
                            "The value is not valid property identifier.",
                            ex
                        );
                    }

                    // Construct domain object.
                    try
                    {
                        return new SimpleSendV0(sender, receiver, property, amount);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw new TransactionFieldException("amount", "The value is not valid amount to send.", ex);
                    }
                default:
                    throw new TransactionFieldException(
                        TransactionFieldException.VersionField,
                        "The value is not valid for simple send."
                    );
            }
        }

        /// <summary>
        /// Read a <see cref="PropertyId"/> from <paramref name="reader"/>.
        /// </summary>
        /// <exception cref="EndOfStreamException">
        /// The end of the stream is reached.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value is not a valid property identifier.
        /// </exception>
        PropertyId DecodePropertyId(BinaryReader reader)
        {
            long value = (uint)IPAddress.NetworkToHostOrder((int)reader.ReadUInt32());

            return new PropertyId(value);
        }

        /// <summary>
        /// Read a <see cref="PropertyAmount"/> from <paramref name="reader"/>.
        /// </summary>
        /// <exception cref="EndOfStreamException">
        /// The end of the stream is reached.
        /// </exception>
        PropertyAmount DecodePropertyAmount(BinaryReader reader)
        {
            var value = IPAddress.NetworkToHostOrder(reader.ReadInt64());

            return new PropertyAmount(value);
        }
    }
}
