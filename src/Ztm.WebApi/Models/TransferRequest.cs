using System.ComponentModel.DataAnnotations;
using NBitcoin;
using Ztm.WebApi.Validators;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Models
{
    public class TransferRequest
    {
        [Required]
        [Positive]
        public PropertyAmount? Amount { get; set; }

        [Required]
        public BitcoinAddress Destination { get; set; }

        [Positive]
        public Money ReferenceAmount { get; set; }
    }
}
