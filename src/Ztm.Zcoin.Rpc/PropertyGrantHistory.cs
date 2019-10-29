using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public class PropertyGrantHistory
    {
        public PropertyGrantType Type { get; set; }
        public uint256 Transaction { get; set; }
        public PropertyAmount Amount { get; set; }
    }
}
