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

        bool IsType(ZcoinOpCode opCode)
        {
            return scriptSig.Length > 0 && scriptSig.ToBytes(true)[0] == (byte)opCode;
        }

        public bool IsZerocoinSpend
        {
            get
            {
                return prevout.IsNull && IsType(ZcoinOpCode.OpZerocoinSpend);
            }
        }

        public bool IsSigmaSpend
        {
            get
            {
                return prevout.Hash == uint256.Zero
                    && prevout.N >= 1
                    && IsType(ZcoinOpCode.OpSigmaSpend);
            }
        }

        public bool IsZerocoinRemint
        {
            get
            {
                return prevout.IsNull && IsType(ZcoinOpCode.OpZerocoinToSigmaRemint);
            }
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }
    }

    enum ZcoinOpCode : byte
    {
        OpZerocoinSpend = 0xc2,
        OpSigmaSpend = 0xc4,
        OpZerocoinToSigmaRemint = 0xc8,
    }
}
