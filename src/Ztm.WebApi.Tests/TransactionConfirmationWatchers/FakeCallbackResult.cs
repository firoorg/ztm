using Ztm.WebApi.Callbacks;

namespace Ztm.WebApi.Tests.TransactionConfirmationWatchers
{
    sealed class FakeCallbackResult : CallbackResult
    {
        public FakeCallbackResult(string status, string data)
        {
            this.Status = status;
            this.Data = data;
        }

        public override string Status { get; }

        public override object Data { get; }
    }
}