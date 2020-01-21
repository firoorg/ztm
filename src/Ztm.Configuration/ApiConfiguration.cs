using System;

namespace Ztm.Configuration
{
    public class ApiConfiguration
    {
        public DefaultApiConfiguration Default { get; set; }
    }

    public class DefaultApiConfiguration
    {
        public int RequiredConfirmation { get; set; }
        public TimeSpan TransactionTimeout { get; set; }
    }
}
