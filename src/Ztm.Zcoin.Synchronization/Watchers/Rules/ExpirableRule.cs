using System;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class ExpirableRule : Rule
    {
        public ExpirableRule()
        {
        }

        public ExpirableRule(Guid id) : base(id)
        {
        }

        public RuleExpirePolicy ExpirePolicy { get; set; }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            return Equals(((ExpirableRule)obj).ExpirePolicy, ExpirePolicy);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
