using System.ComponentModel.DataAnnotations;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Validators
{
    public sealed class PositiveAttribute : ValidationAttribute
    {
        public PositiveAttribute()
            : base("The value must be positive.")
        {
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            switch (value)
            {
                case PropertyAmount a:
                    if (a >= PropertyAmount.Zero)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}
