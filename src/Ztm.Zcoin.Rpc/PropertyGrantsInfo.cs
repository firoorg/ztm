using System.Collections.Generic;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public class PropertyGrantsInfo
    {
        public PropertyId Id { get; set; }
        public string Name { get; set; }
        public BitcoinAddress Issuer { get; set; }
        public uint256 CreationTransaction { get; set; }
        public PropertyAmount TotalTokens { get; set; }
        public IEnumerable<PropertyGrantHistory> Histories { get; set; }
    }
}
