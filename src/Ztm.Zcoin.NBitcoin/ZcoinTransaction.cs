using System;
using System.Linq;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    sealed class ZcoinTransaction : Transaction
    {
        readonly ZcoinConsensusFactory consensusFactory;

        #pragma warning disable CS0618
        public ZcoinTransaction(ZcoinConsensusFactory consensusFactory)
        {
            if (consensusFactory == null)
            {
                throw new ArgumentNullException(nameof(consensusFactory));
            }

            this.consensusFactory = consensusFactory;
        }
        #pragma warning restore CS0618

        public bool IsZerocoinSpend
        {
            get
            {
                return Inputs.Any(txIn => txIn.IsZerocoinSpend());
            }
        }

        public bool IsSigmaSpend
        {
            get
            {
                return Inputs.Any(txIn => txIn.IsSigmaSpend());
            }
        }

        public bool IsZerocoinRemint
        {
            get
            {
                return Inputs.Any(txIn => txIn.IsZerocoinRemint());
            }
        }

        public override bool IsCoinBase
        {
            get
            {
                if (IsZerocoinSpend || IsSigmaSpend || IsZerocoinRemint)
                {
                    return false;
                }

                return base.IsCoinBase;
            }
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }
    }
}
