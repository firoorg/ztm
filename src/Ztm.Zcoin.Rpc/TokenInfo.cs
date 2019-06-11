using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Rpc
{
    public class TokenInfo
    {
        public TokenId Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public TokenType Type { get; set; }
    }
}
