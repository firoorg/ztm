using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Testing
{
    public static class TestBlock
    {
        public static readonly Block Regtest0 = ZcoinNetworks.Instance.Regtest.GetGenesis();
        public static readonly Block Regtest1 = Regtest0.CreateNextBlockWithCoinbase(TestAddress.Regtest1, 1);
        public static readonly Block Regtest2 = Regtest1.CreateNextBlockWithCoinbase(TestAddress.Regtest2, 2);
        public static readonly Block Regtest3 = Regtest2.CreateNextBlockWithCoinbase(TestAddress.Regtest3, 3);
    }
}
