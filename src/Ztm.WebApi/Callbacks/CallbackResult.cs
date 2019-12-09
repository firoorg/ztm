namespace Ztm.WebApi.Callbacks
{
    public abstract class CallbackResult
    {
        public const string StatusError   = "error";
        public const string StatusSuccess = "success";
        public const string StatusUpdate  = "update";

        public abstract string Status { get; }
        public abstract object Data { get; }
    }
}