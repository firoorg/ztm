using System;
using Xunit;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public class TheoryWhenEnvIsSetAttribute : TheoryAttribute
    {
        public TheoryWhenEnvIsSetAttribute(string name)
        {
            if (Environment.GetEnvironmentVariable(name) == null)
            {
                Skip = $"No {name} environment variable is set.";
            }
        }
    }
}