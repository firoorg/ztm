using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public interface ITransactionEncoder
    {
        byte[] Encode(ExodusTransaction transaction);
        ExodusTransaction Decode(BitcoinAddress sender, BitcoinAddress receiver, byte[] data);
    }
}
