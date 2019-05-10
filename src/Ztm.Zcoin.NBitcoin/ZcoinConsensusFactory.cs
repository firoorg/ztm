using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class ZcoinConsensusFactory : ConsensusFactory
    {
        private ZcoinConsensusFactory()
        {
        }

        public static ZcoinConsensusFactory Instance { get; } = new ZcoinConsensusFactory();

        public override Block CreateBlock()
        {
            return new ZcoinBlock(new ZcoinBlockHeader());
        }

        public override BlockHeader CreateBlockHeader()
        {
            return new ZcoinBlockHeader();
        }
    }
}
