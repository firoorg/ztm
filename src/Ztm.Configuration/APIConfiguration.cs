using System;

namespace Ztm.Configuration
{
    public class APIConfiguration
    {
        public GlobalConfiguration Global { get; set; }
    }

    public class GlobalConfiguration
    {
        public int RequiredConfirmation { get; set; }
        public TimeSpan TransactionTimeout { get; set; }
    }
}