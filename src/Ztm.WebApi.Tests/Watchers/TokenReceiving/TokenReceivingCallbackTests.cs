using System;
using System.Collections.Generic;
using System.Net;
using Xunit;
using Ztm.Testing;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TokenReceiving;

namespace Ztm.WebApi.Tests.Watchers.TokenReceiving
{
    public sealed class TokenReceivingCallbackTests
    {
        readonly Callback callback;
        readonly TokenReceivingCallback subject;
        readonly IEnumerable<TokenReceivingCallback> equals;
        readonly IEnumerable<TokenReceivingCallback> unequals;

        public TokenReceivingCallbackTests()
        {
            this.callback = new Callback(
                Guid.NewGuid(),
                IPAddress.Parse("192.168.1.2"),
                DateTime.Now,
                true,
                new Uri("http://localhost/a"));

            this.subject = new TokenReceivingCallback(this.callback, "timeout");

            this.equals = new[]
            {
                new TokenReceivingCallback(this.subject.Callback, this.subject.TimeoutStatus),
            };

            this.unequals = new[]
            {
                new TokenReceivingCallback(
                    new Callback(
                        Guid.NewGuid(),
                        IPAddress.Parse("192.168.1.3"),
                        DateTime.Now,
                        false,
                        new Uri("http://localhost/b")),
                    this.subject.TimeoutStatus),
                new TokenReceivingCallback(this.subject.Callback, ""),
            };
        }

        [Fact]
        public void Constructor_WithNullCallback_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("callback", () => new TokenReceivingCallback(null, "timeout"));
        }

        [Fact]
        public void Constructor_WithNullTimeoutStatus_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "timeoutStatus",
                () => new TokenReceivingCallback(this.callback, null));
        }

        [Fact]
        public void Constructor_WhenSucceeded_ShouldInitializeProperties()
        {
            Assert.Equal(this.callback, this.subject.Callback);
            Assert.Equal(this.callback.Completed, this.subject.Completed);
            Assert.Equal("timeout", this.subject.TimeoutStatus);
        }

        [Fact]
        public void Equality_WithSameInstance_ShoudReturnTrue()
        {
            var left = this.subject;
            var right = this.subject;

            Assert.True(left == right);
        }

        [Fact]
        public void Equality_WithBothNull_ShoudReturnTrue()
        {
            TokenReceivingCallback left, right;

            left = null;
            right = null;

            Assert.True(left == right);
        }

        [Fact]
        public void Equality_WithLeftNull_ShoudReturnFalse()
        {
            Assert.False(null == this.subject);
        }

        [Fact]
        public void Equality_WithRightNull_ShoudReturnFalse()
        {
            Assert.False(this.subject == null);
        }

        [Fact]
        public void Equality_WithEquals_ShoudReturnTrue()
        {
            foreach (var v in this.equals)
            {
                Assert.True(this.subject == v);
            }
        }

        [Fact]
        public void Equality_WithUnequals_ShoudReturnFalse()
        {
            foreach (var v in this.unequals)
            {
                Assert.False(this.subject == v);
            }
        }

        [Fact]
        public void Inequality_WithSameInstance_ShoudReturnFalse()
        {
            var left = this.subject;
            var right = this.subject;

            Assert.False(left != right);
        }

        [Fact]
        public void Inequality_WithBothNull_ShoudReturnFalse()
        {
            TokenReceivingCallback left, right;

            left = null;
            right = null;

            Assert.False(left != right);
        }

        [Fact]
        public void Inequality_WithLeftNull_ShoudReturnTrue()
        {
            Assert.True(null != this.subject);
        }

        [Fact]
        public void Inequality_WithRightNull_ShoudReturnTrue()
        {
            Assert.True(this.subject != null);
        }

        [Fact]
        public void Inequality_WithEquals_ShoudReturnFalse()
        {
            foreach (var v in this.equals)
            {
                Assert.False(this.subject != v);
            }
        }

        [Fact]
        public void Inequality_WithUnequals_ShoudReturnTrue()
        {
            foreach (var v in this.unequals)
            {
                Assert.True(this.subject != v);
            }
        }

        [Fact]
        public void Equals_WithEqual_ShouldReturnTrue()
        {
            var results = EqualityTesting.TestEquals(this.subject, this.equals);

            Assert.DoesNotContain(false, results);
        }

        [Fact]
        public void Equals_WithUnequal_ShouldReturnFalse()
        {
            var results = EqualityTesting.TestInequal(this.subject, this.unequals);

            Assert.DoesNotContain(true, results);
        }
    }
}
