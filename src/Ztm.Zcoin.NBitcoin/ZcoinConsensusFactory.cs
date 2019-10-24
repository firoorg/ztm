using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class ZcoinConsensusFactory : ConsensusFactory
    {
        public ZcoinConsensusFactory(DateTimeOffset mtpSwitchTime)
        {
            MtpSwitchTime = mtpSwitchTime;
        }

        public DateTimeOffset MtpSwitchTime { get; }

        public override Block CreateBlock()
        {
            return new ZcoinBlock(this, (ZcoinBlockHeader)CreateBlockHeader());
        }

        public override BlockHeader CreateBlockHeader()
        {
            return new ZcoinBlockHeader(MtpSwitchTime);
        }

        public override Transaction CreateTransaction()
        {
            return new ZcoinTransaction(this);
        }

        public override TxIn CreateTxIn()
        {
            return new ZcoinTxIn(this);
        }

        public override TxOut CreateTxOut()
        {
            return new ZcoinTxOut(this);
        }
    }
}
