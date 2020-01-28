using System;
using Ztm.Threading;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public sealed class Watching
    {
        public Watching(Rule rule, Timer timer)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (timer == null)
            {
                throw new ArgumentNullException(nameof(timer));
            }

            Rule = rule;
            Timer = timer;
        }

        public Rule Rule { get; }

        public Timer Timer { get; }

        public void Deconstruct(out Rule rule, out Timer timer)
        {
            rule = Rule;
            timer = Timer;
        }
    }
}
