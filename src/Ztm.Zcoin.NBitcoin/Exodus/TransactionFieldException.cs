using System;

namespace Ztm.Zcoin.NBitcoin.Exodus
{
    public class TransactionFieldException : TransactionException
    {
        public const string TypeField = "type";
        public const string VersionField = "version";

        public TransactionFieldException(string field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            Field = field;
        }

        public TransactionFieldException(string field, string message) : base(message)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            Field = field;
        }

        public TransactionFieldException(string field, string message, Exception innerException)
            : base(message, innerException)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            Field = field;
        }

        public string Field { get; }
    }
}
