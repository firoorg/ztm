using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public static class TxInExtensions
    {
        public static bool IsZerocoinSpend(this TxIn txIn)
        {
            return ((ZcoinTxIn)txIn).IsZerocoinSpend;
        }

        public static bool IsSigmaSpend(this TxIn txIn)
        {
            return ((ZcoinTxIn)txIn).IsSigmaSpend;
        }

        public static bool IsZerocoinRemint(this TxIn txIn)
        {
            return ((ZcoinTxIn)txIn).IsZerocoinRemint;
        }
    }
}