using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public class ZcoinTransaction : Transaction
    {
        #pragma warning disable CS0618
        public ZcoinTransaction()
        {
        }
        #pragma warning restore CS0618

        public static new ZcoinTransaction Parse(string hex, Network network)
        {
            return (ZcoinTransaction)Transaction.Parse(hex, network);
        }
    }
}
