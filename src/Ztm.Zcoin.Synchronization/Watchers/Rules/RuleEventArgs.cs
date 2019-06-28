using System;
using System.Threading;
using Ztm.ObjectModel;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public class RuleEventArgs<T> : AsyncEventArgs where T : Rule
    {
        public RuleEventArgs(T rule, CancellationToken cancellationToken) : base(cancellationToken)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            Rule = rule;
        }

        public T Rule { get; }
    }
}
