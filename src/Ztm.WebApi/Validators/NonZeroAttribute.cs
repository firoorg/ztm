using System.ComponentModel.DataAnnotations;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Validators
{
    public sealed class NonZeroAttribute : ValidationAttribute
    {
        public NonZeroAttribute()
            : base("The value cannot be zero.")
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
                    if (!a.Equals(PropertyAmount.Zero))
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}
