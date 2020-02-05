using System;
using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public sealed class TokenReceivingCallback
    {
        public TokenReceivingCallback(Callback callback, string timeoutStatus)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (timeoutStatus == null)
            {
                throw new ArgumentNullException(nameof(timeoutStatus));
            }

            Callback = callback;
            TimeoutStatus = timeoutStatus;
        }

        public Callback Callback { get; }

        public bool Completed => Callback.Completed;

        public string TimeoutStatus { get; }

        public static bool operator ==(TokenReceivingCallback left, TokenReceivingCallback right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(TokenReceivingCallback left, TokenReceivingCallback right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            var other = obj as TokenReceivingCallback;

            if (other == null)
            {
                return false;
            }

            return other.Callback.Equals(Callback) && other.TimeoutStatus == TimeoutStatus;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Callback, TimeoutStatus);
        }
    }
}
