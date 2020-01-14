using System;
using Xunit;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public class ConditionalTheoryAttribute : TheoryAttribute
    {
        public string RequiredEnv { get; set; }

        public override string Skip
        {
            get
            {
                if (RequiredEnv != null && Environment.GetEnvironmentVariable(RequiredEnv) == null)
                {
                    return $"No {RequiredEnv} environment variable is set.";
                }

                return null;
            }
        }
    }
}