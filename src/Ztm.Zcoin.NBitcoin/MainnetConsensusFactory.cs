using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class MainnetConsensusFactory : ZcoinConsensusFactory
    {
        readonly DateTimeOffset MtpSwitchTime = DateTimeOffset.FromUnixTimeSeconds(1544443200);

        private MainnetConsensusFactory()
        {
        }

        public static MainnetConsensusFactory Instance { get; } = new MainnetConsensusFactory();

        public override BlockHeader CreateBlockHeader()
        {
            return new ZcoinBlockHeader(MtpSwitchTime);
        }
    }
}
