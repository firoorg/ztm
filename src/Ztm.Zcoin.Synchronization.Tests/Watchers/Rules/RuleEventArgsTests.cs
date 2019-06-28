using System;
using System.Threading;
using Xunit;
using Ztm.Zcoin.Synchronization.Watchers.Rules;

namespace Ztm.Zcoin.Synchronization.Tests.Watchers.Rules
{
    public class RuleEventArgsTests
    {
        [Fact]
        public void Constructor_WithNullRule_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "rule",
                () => new RuleEventArgs<Rule>(null, CancellationToken.None)
            );
        }

        [Fact]
        public void Constructor_WithNonNullRule_ShouldAssignToRuleProperty()
        {
            var rule = new Rule();
            var subject = new RuleEventArgs<Rule>(rule, CancellationToken.None);

            Assert.Same(rule, subject.Rule);
        }
    }
}
