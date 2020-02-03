using System.ComponentModel.DataAnnotations;
using Ztm.WebApi.Validators;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Models
{
    public class ReceivingRequest
    {
        [Required]
        [Positive]
        public PropertyAmount? TargetAmount { get; set; }
    }
}
