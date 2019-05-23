using System;
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

        public override Transaction CreateTransaction()
        {
            return new ZcoinTransaction();
        }

        public TxIn CreateTxIn()
        {
            return new ZcoinTxIn();
        }

        public override TxOut CreateTxOut()
        {
            return new ZcoinTxOut();
        }

        public override bool TryCreateNew(Type type, out IBitcoinSerializable result)
        {
            if (base.TryCreateNew(type, out result))
            {
                return true;
            }

            if (typeof(TxIn).IsAssignableFrom(type))
            {
                result = CreateTxIn();
                return true;
            }

            return false;
        }
    }
}
