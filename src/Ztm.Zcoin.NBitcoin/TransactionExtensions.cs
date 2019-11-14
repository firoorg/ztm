using System;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

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

        [ObsoleteAttribute("This method should be called in specific places", false)]
        public static void SetExodusTransaction(this Transaction transaction, ExodusTransaction exodusTransaction)
        {
            ((ZcoinTransaction)transaction).ExodusTransaction = exodusTransaction;
        }

        public static ExodusTransaction GetExodusTransaction(this Transaction transaction)
        {
            return ((ZcoinTransaction)transaction).ExodusTransaction;
        }
    }
}