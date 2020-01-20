using System;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public class CallbackData
    {
        public int? Confirmation { get; set; }

        public PropertyAmount? Received { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as CallbackData;

            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return other.Confirmation == Confirmation && other.Received == Received;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Confirmation, Received);
        }
    }
}
