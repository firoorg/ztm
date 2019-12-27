using System;

namespace Ztm.Configuration
{
    public class CallbackConfiguration
    {
        public TransactionConfirmation TransactionConfirmation { get; set; }
    }

    public class TransactionConfirmation
    {
        public TimeSpan Timeout { get; set; }
        public int RequiredConfirmation { get; set; }
    }
}