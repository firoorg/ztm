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

        bool IsStartsWith(ZcoinOpCode opCode)
        {
            return scriptSig.Length > 0 && scriptSig.ToBytes(true)[0] == (byte)opCode;
        }

        public bool IsZerocoinSpend
        {
            get
            {
                return prevout.IsNull && IsStartsWith(ZcoinOpCode.ZerocoinSpend);
            }
        }

        public bool IsSigmaSpend
        {
            get
            {
                return prevout.Hash == uint256.Zero
                    && prevout.N >= 1
                    && IsStartsWith(ZcoinOpCode.SigmaSpend);
            }
        }

        public bool IsZerocoinRemint
        {
            get
            {
                return prevout.IsNull && IsStartsWith(ZcoinOpCode.ZerocoinToSigmaRemint);
            }
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }
    }
}
