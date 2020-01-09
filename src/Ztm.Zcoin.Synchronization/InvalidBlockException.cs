using System;

namespace Ztm.Zcoin.Synchronization
{
    public class InvalidBlockException : Exception
    {
        public InvalidBlockException() : base()
        {
        }

        public InvalidBlockException(string message) : base(message)
        {
        }

        public InvalidBlockException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}