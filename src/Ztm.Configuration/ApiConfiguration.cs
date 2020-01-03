using System;

namespace Ztm.Configuration
{
    public class ApiConfiguration
    {
        public DefaultConfiguration Default { get; set; }
    }

    public class DefaultConfiguration
    {
        public int RequiredConfirmation { get; set; }
        public TimeSpan TransactionTimeout { get; set; }
    }
}