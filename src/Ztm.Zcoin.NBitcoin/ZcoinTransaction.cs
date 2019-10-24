using System;
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
                foreach (var txIn in Inputs)
                {
                    if (txIn.IsZerocoinSpend())
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsSigmaSpend
        {
            get
            {
                foreach (var txIn in Inputs)
                {
                    if (txIn.IsSigmaSpend())
                    {
                        return true;
                    }

                }

                return false;
            }
        }

        public bool IsZerocoinRemint
        {
            get
            {
                foreach (var txIn in Inputs)
                {
                    if (txIn.IsZerocoinRemint())
                    {
                        return true;
                    }
                }

                return false;
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
