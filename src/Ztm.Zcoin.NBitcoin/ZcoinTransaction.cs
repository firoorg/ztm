using System;
using System.Linq;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

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

        public ExodusTransaction ExodusTransaction { get; set; }

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

        public bool IsSigmaSpend
        {
            get
            {
                return Inputs.All(txIn => txIn.IsSigmaSpend());
            }
        }

        public bool IsZerocoinRemint
        {
            get
            {
                return Inputs.All(txIn => txIn.IsZerocoinRemint());
            }
        }

        public bool IsZerocoinSpend
        {
            get
            {
                return Inputs.All(txIn => txIn.IsZerocoinSpend());
            }
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }
    }
}
