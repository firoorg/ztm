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
                return prevout.IsNull && scriptSig.Length > 0 && scriptSig.ToBytes()[0] == OpZerocoinSpend;
            }
        }

        public bool IsSigmaSpend
        {
            get
            {
                return prevout.Hash == uint256.Zero
                    && prevout.N >= 1
                    && scriptSig.Length > 0
                    && scriptSig.ToBytes()[0] == OpSigmaSpend;
            }
        }

        public bool IsZerocoinRemint
        {
            get
            {
                return prevout.IsNull && scriptSig.Length > 0 && scriptSig.ToBytes()[0] == OpZerocoinToSigmaRemint;
            }
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }

        public static readonly byte OpZerocoinSpend = 0xc2;
        public static readonly byte OpSigmaSpend = 0xc4;
        public static readonly byte OpZerocoinToSigmaRemint = 0xc8;
    }
}
