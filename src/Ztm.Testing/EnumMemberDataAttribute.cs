using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace Ztm.Testing
{
    public sealed class EnumMemberDataAttribute : DataAttribute
    {
        public EnumMemberDataAttribute(Type type, params object[] exclude)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsEnum)
            {
                throw new ArgumentException($"{type} is not enum.", nameof(type));
            }

            Type = type;
            Exclude = new HashSet<object>(exclude);
        }

        public ISet<object> Exclude { get; }

        public Type Type { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return Enum.GetValues(Type)
                .Cast<object>()
                .Where(v => !Exclude.Contains(v))
                .Select(v => new[] { v })
                .ToList();
        }
    }
}
