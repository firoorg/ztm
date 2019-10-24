using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class ZcoinTxIn : TxIn
    {
        readonly ZcoinConsensusFactory consensusFactory;

        public ZcoinTxIn(ZcoinConsensusFactory consensusFactory)
        {
            if (consensusFactory == null)
            {
                throw new ArgumentNullException(nameof(consensusFactory));
            }

            this.consensusFactory = consensusFactory;
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }
    }
}
