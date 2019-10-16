namespace Ztm.WebApi
{
    public abstract class CallbackResult
    {
        public static class CallbackStatus
        {
            public const string Error   = "error";
            public const string Success = "success";
            public const string Update  = "update";
        }

        public CallbackResult(string status, object data)
        {
            this.Status = status;
            this.Data = data;
        }

        public string Status { get; }
        public object Data { get; }
    }
}