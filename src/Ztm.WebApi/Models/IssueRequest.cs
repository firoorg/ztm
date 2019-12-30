using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Models
{
    public sealed class IssueRequest
    {
        public PropertyAmount Amount { get; set; }
        public string Note { get; set; }
    }
}