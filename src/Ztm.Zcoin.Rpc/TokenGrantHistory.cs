using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public class TokenGrantHistory
    {
        public TokenGrantType Type { get; set; }
        public uint256 Transaction { get; set; }
        public TokenAmount Amount { get; set; }
    }
}
