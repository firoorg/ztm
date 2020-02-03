using System;
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
            switch (value)
            {
                case PropertyAmount a:
                    if (a == PropertyAmount.Zero)
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
