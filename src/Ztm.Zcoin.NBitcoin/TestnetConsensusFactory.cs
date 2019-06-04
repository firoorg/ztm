using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class TestnetConsensusFactory : ZcoinConsensusFactory
    {
        static readonly DateTimeOffset MtpSwitchTime = DateTimeOffset.FromUnixTimeSeconds(1539172800);

        private TestnetConsensusFactory()
        {
        }

        public static TestnetConsensusFactory Instance { get; } = new TestnetConsensusFactory();

        public override BlockHeader CreateBlockHeader()
        {
            return new ZcoinBlockHeader(MtpSwitchTime);
        }
    }
}
