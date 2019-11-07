using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    sealed class TestExodusTransaction : ExodusTransaction
    {
        public TestExodusTransaction(BitcoinAddress sender, BitcoinAddress receiver) : base(sender, receiver)
        {
        }

        public override int Id => 1;

        public override int Version => 1;
    }
}
