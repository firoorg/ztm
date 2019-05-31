using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class RegtestConsensusFactory : ZcoinConsensusFactory
    {
        private RegtestConsensusFactory()
        {
        }

        public static RegtestConsensusFactory Instance { get; } = new RegtestConsensusFactory();

        public override BlockHeader CreateBlockHeader()
        {
            return new ZcoinBlockHeader(DateTimeOffset.MaxValue);
        }
    }
}
