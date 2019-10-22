using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class ZcoinBlock : Block
    {
        readonly ZcoinConsensusFactory consensusFactory;

        #pragma warning disable CS0618
        public ZcoinBlock(ZcoinConsensusFactory consensusFactory, ZcoinBlockHeader header) : base(header)
        {
            if (consensusFactory == null)
            {
                throw new ArgumentNullException(nameof(consensusFactory));
            }

            this.consensusFactory = consensusFactory;
        }
        #pragma warning restore CS0618

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }
    }
}
