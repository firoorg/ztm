using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class ZcoinConsensusFactory : ConsensusFactory
    {
        private ZcoinConsensusFactory()
        {
        }

        public static ZcoinConsensusFactory Instance { get; } = new ZcoinConsensusFactory();
    }
}
