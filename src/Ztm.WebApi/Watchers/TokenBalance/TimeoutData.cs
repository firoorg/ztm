using System;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public class TimeoutData : CallbackData
    {
        public int TargetConfirmation { get; set; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            return ((TimeoutData)obj).TargetConfirmation == TargetConfirmation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), TargetConfirmation);
        }
    }
}
