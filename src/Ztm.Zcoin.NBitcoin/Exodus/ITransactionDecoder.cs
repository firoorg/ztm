using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public interface ITransactionDecoder
    {
        ExodusTransaction Decode(BitcoinAddress sender, BitcoinAddress receiver, byte[] data);
    }
}
