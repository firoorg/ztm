using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    sealed class FakeExodusTransaction : ExodusTransaction
    {
        public FakeExodusTransaction(BitcoinAddress sender, BitcoinAddress receiver) : base(sender, receiver)
        {
            Id = 1;
            Version = 1;
        }

        public FakeExodusTransaction(BitcoinAddress sender, BitcoinAddress receiver, int id, int version)
            : base(sender, receiver)
        {
            Id = id;
            Version = version;
        }

        public override int Id { get; }

        public override int Version { get; }
    }
}
