using System;
using Xunit;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public class ConditionalTheoryAttribute : TheoryAttribute
    {
        public virtual string RequiredEnv
        {
            get
            {
                return _RequiredEnv;
            }

            set
            {
                _RequiredEnv = value;

                if (Environment.GetEnvironmentVariable(value) == null)
                {
                    Skip = $"No {value} environment variable is set.";
                }
            }
        }

        string _RequiredEnv;
    }
}