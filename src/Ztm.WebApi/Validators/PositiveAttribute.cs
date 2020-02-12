using System;
using System.ComponentModel.DataAnnotations;
using NBitcoin;
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
            switch (value)
            {
                case PropertyAmount a:
                    if (a <= PropertyAmount.Zero)
                    {
                        return false;
                    }
                    break;
                case Money m:
                    if (m <= Money.Zero)
                    {
                        return false;
                    }
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentException($"Type {value.GetType()} is not supported.", nameof(value));
            }

            return true;
        }
    }
}
