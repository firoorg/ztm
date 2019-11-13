using System;
using System.IO;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public sealed class SimpleSendEncoder : TransactionPayloadEncoder
    {
        public override int Type => SimpleSendV0.StaticId;

        protected override ExodusTransaction Decode(
            BitcoinAddress sender,
            BitcoinAddress receiver,
            BinaryReader payload,
            int version)
        {
            PropertyId property;
            PropertyAmount amount;

            switch (version)
            {
                case 0:
                    // Parse.
                    try
                    {
                        property = DecodePropertyId(payload);
                        amount = DecodePropertyAmount(payload);
                    }
                    catch (EndOfStreamException ex)
                    {
                        throw new TransactionPayloadTooShortException(12, ex);
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
    }
}
