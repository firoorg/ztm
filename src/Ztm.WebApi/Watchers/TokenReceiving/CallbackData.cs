namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public sealed class CallbackData
    {
        public CallbackAmount Received { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as CallbackData;

            if (other == null)
            {
                return false;
            }

            return other.Received == Received;
        }

        public override int GetHashCode()
        {
            return Received?.GetHashCode() ?? 0;
        }
    }
}
