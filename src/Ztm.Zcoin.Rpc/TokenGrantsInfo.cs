using System.Collections.Generic;
using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public class TokenGrantsInfo
    {
        public TokenId Id { get; set; }
        public string Name { get; set; }
        public BitcoinAddress Issuer { get; set; }
        public uint256 CreationTransaction { get; set; }
        public TokenAmount? TotalTokens { get; set; }
        public IEnumerable<TokenGrantHistory> Histories { get; set; }
    }
}
