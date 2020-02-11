using System.ComponentModel.DataAnnotations;
using Ztm.WebApi.Validators;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Models
{
    public sealed class IssueTokenRequest
    {
        [Required]
        [Positive]
        public PropertyAmount? Amount { get; set; }

        public string Note { get; set; }
    }
}
