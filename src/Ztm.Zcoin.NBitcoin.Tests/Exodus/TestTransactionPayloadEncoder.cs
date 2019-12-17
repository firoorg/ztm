using System;
using System.IO;
using NBitcoin;
using NSubstitute;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    sealed class TestTransactionPayloadEncoder : TransactionPayloadEncoder
    {
        public TestTransactionPayloadEncoder()
        {
            FakeDecode = Substitute.For<Func<BitcoinAddress, BitcoinAddress, BinaryReader, int, ExodusTransaction>>();
            FakeType = Substitute.For<Func<int>>();

            FakeDecode(
                Arg.Any<BitcoinAddress>(),
                Arg.Any<BitcoinAddress>(),
                Arg.Any<BinaryReader>(),
                Arg.Any<int>()
            ).Returns(call => new FakeExodusTransaction(
                call.ArgAt<BitcoinAddress>(0),
                call.ArgAt<BitcoinAddress>(1),
                Type,
                call.ArgAt<int>(3)
            ));
        }

        public Func<BitcoinAddress, BitcoinAddress, BinaryReader, int, ExodusTransaction> FakeDecode { get; }

        public Func<int> FakeType { get; }

        public override int Type => FakeType();

        public new static PropertyAmount DecodePropertyAmount(BinaryReader reader)
        {
            return TransactionPayloadEncoder.DecodePropertyAmount(reader);
        }

        public new static PropertyId DecodePropertyId(BinaryReader reader)
        {
            return TransactionPayloadEncoder.DecodePropertyId(reader);
        }

        public ExodusTransaction InvokeDecode(
            BitcoinAddress sender,
            BitcoinAddress receiver,
            BinaryReader payload,
            int version)
        {
            return ((ITransactionPayloadEncoder)this).Decode(sender, receiver, payload, version);
        }

        protected override ExodusTransaction Decode(
            BitcoinAddress sender,
            BitcoinAddress receiver,
            BinaryReader payload,
            int version)
        {
            return FakeDecode(sender, receiver, payload, version);
        }
    }
}
