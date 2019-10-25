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

        public bool IsZerocoinSpend
        {
            get
            {
                return prevout.IsNull && ScriptStartsWith(ZcoinOpCode.ZerocoinSpend);
            }
        }

        public bool IsSigmaSpend
        {
            get
            {
                return prevout.Hash == uint256.Zero
                    && prevout.N >= 1
                    && ScriptStartsWith(ZcoinOpCode.SigmaSpend);
            }
        }

        public bool IsZerocoinRemint
        {
            get
            {
                return prevout.IsNull && ScriptStartsWith(ZcoinOpCode.ZerocoinToSigmaRemint);
            }
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }

        bool ScriptStartsWith(ZcoinOpCode opCode)
        {
            return scriptSig.Length > 0 && scriptSig.ToBytes(true)[0] == (byte)opCode;
        }
    }
}
