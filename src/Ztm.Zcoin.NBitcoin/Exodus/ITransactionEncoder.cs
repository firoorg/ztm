using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public interface ITransactionEncoder
    {
        ExodusTransaction Decode(BitcoinAddress sender, BitcoinAddress receiver, byte[] data);
    }
}
