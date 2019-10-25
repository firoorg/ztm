using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public static class TransactionExtensions
    {
        public static bool IsZerocoinSpend(this Transaction transaction)
        {
            return ((ZcoinTransaction)transaction).IsZerocoinSpend;
        }

        public static bool IsSigmaSpend(this Transaction transaction)
        {
            return ((ZcoinTransaction)transaction).IsSigmaSpend;
        }

        public static bool IsZerocoinRemint(this Transaction transaction)
        {
            return ((ZcoinTransaction)transaction).IsZerocoinRemint;
        }
    }
}