using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public abstract class ZcoinConsensusFactory : ConsensusFactory
    {
        protected ZcoinConsensusFactory()
        {
        }

        public override Block CreateBlock()
        {
            return new ZcoinBlock(this, CreateBlockHeader());
        }

        public override abstract BlockHeader CreateBlockHeader();

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
