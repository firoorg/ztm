using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Ztm.ObjectModel;
using Ztm.ServiceModel;
using Ztm.Threading;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Watchers.Rules
{
    public abstract class ExpirableRulesExecutor<TRule, TWatch> : BackgroundService, IExpirableRulesExecutor<TRule, TWatch>
        where TRule : ExpirableRule
        where TWatch : RuledWatch<TRule>
    {
        readonly IExpirableRulesStorage<TRule> storage;
        readonly IRulesExpireWatcher<TRule, TWatch> expireWatcher;
        readonly ShutdownGuard shutdownGuard;
        bool disposed;

        protected ExpirableRulesExecutor(
            IExpirableRulesStorage<TRule> storage,
            IRulesExpireWatcher<TRule, TWatch> expireWatcher)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (expireWatcher == null)
            {
                throw new ArgumentNullException(nameof(expireWatcher));
            }

            this.storage = storage;
            this.expireWatcher = expireWatcher;
            this.expireWatcher.RuleExpired += (sender, e) =>
            {
                e.RegisterBackgroundTask(cancellationToken => InvokeRuleExpiredAsync(e, cancellationToken));
            };
            this.shutdownGuard = new ShutdownGuard();
        }

        public event EventHandler<RuleEventArgs<TRule>> RuleExpired;

        protected abstract Task<bool> DisassociateRuleAsyc(
            TWatch watch,
            WatchRemoveReason reason,
            CancellationToken cancellationToken);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.shutdownGuard.Dispose();
            }

            this.disposed = true;
        }

        protected abstract Task<IEnumerable<TWatch>> ExecuteRulesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken);

        protected override async Task OnStartAsync(CancellationToken cancellationToken)
        {
            await this.expireWatcher.StartAsync(cancellationToken);

            // Resume expiration watcher. We need to load all previous rule here because we don't want it to mix with
            // the new one.
            var rules = await this.storage.GetRulesAsync(cancellationToken);

            // We need to resume expiration watcher after this watcher is started to prevent it invoke handler before
            // this watcher is fully started.
            Started += (sender, e) => e.RegisterBackgroundTask(async (_) =>
            {
                try
                {
                    foreach (var rule in rules)
                    {
                        if (rule.ExpirePolicy == null)
                        {
                            continue;
                        }

                        await this.expireWatcher.AddRuleAsync(rule, CancellationToken.None);
                    }
                }
                catch
                {
                    // Ignore.
                }
            });
        }

        protected override async Task OnStopAsync(CancellationToken cancellationToken)
        {
            // Disable adding new rules so no new expiration watcher will be started after this.
            await this.shutdownGuard.SetAndWaitAsync(cancellationToken);
            await this.expireWatcher.StopAsync(cancellationToken);
        }

        async Task InvokeRuleExpiredAsync(RuleEventArgs<TRule> e, CancellationToken cancellationToken)
        {
            await RuleExpired.InvokeAsync(this, e);
            await this.storage.RemoveRulesAsync(new[] { e.Rule }, CancellationToken.None);
        }

        async Task IRulesExecutor<TRule, TWatch>.AddRuleAsync(TRule rule, CancellationToken cancellationToken)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (rule.ExpirePolicy != null && !this.expireWatcher.IsSupported(rule.ExpirePolicy.GetType()))
            {
                throw new ArgumentException(
                    $"Expire policy {rule.ExpirePolicy.GetType()} is not supported.",
                    nameof(rule)
                );
            }

            // Ensure we are not in the initialization/finalization state to prevent incosistent expiration watcher.
            if (!IsRunning || !this.shutdownGuard.TryLock())
            {
                throw new InvalidOperationException("The executor is not running.");
            }

            try
            {
                await this.storage.AddRuleAsync(rule, cancellationToken);

                try
                {
                    if (rule.ExpirePolicy == null)
                    {
                        return;
                    }

                    await this.expireWatcher.AddRuleAsync(rule, CancellationToken.None);
                }
                catch
                {
                    // Ignore.
                }
            }
            finally
            {
                this.shutdownGuard.Release();
            }
        }

        async Task IRulesExecutor<TRule, TWatch>.DisassociateRulesAsyc(
            IEnumerable<WatchToRemove<TWatch>> watches,
            CancellationToken cancellationToken)
        {
            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            if (!IsRunning || !this.shutdownGuard.TryLock())
            {
                throw new InvalidOperationException("The executor is not running.");
            }

            try
            {
                // Determine if rules need to be remove.
                var removes = new Collection<TRule>();

                foreach (var watch in watches)
                {
                    var removeRule = await DisassociateRuleAsyc(watch.Watch, watch.Reason, cancellationToken);

                    if (watch.Watch.Rule.ExpirePolicy != null)
                    {
                        await this.expireWatcher.RemoveReferenceAsync(watch.Watch, !removeRule, cancellationToken);
                    }

                    if (removeRule)
                    {
                        removes.Add(watch.Watch.Rule);
                    }
                }

                // Remove rules if there is one in the list.
                if (removes.Count > 0)
                {
                    await this.storage.RemoveRulesAsync(removes, cancellationToken);
                }
            }
            finally
            {
                this.shutdownGuard.Release();
            }
        }

        async Task<IEnumerable<TWatch>> IRulesExecutor<TRule, TWatch>.ExecuteRulesAsync(
            ZcoinBlock block,
            int height,
            CancellationToken cancellationToken)
        {
            if (!IsRunning || !this.shutdownGuard.TryLock())
            {
                throw new InvalidOperationException("The executor is not running.");
            }

            try
            {
                var watches = new Collection<TWatch>();

                foreach (var watch in await ExecuteRulesAsync(block, height, cancellationToken))
                {
                    if (watch.Rule.ExpirePolicy != null)
                    {
                        if (!await this.expireWatcher.AddReferenceAsync(watch, cancellationToken))
                        {
                            continue;
                        }
                    }

                    watches.Add(watch);
                }

                return watches;
            }
            finally
            {
                this.shutdownGuard.Release();
            }
        }
    }
}
