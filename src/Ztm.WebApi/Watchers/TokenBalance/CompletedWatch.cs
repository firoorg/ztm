using System;
using Watch = Ztm.Zcoin.Watching.BalanceWatch<Ztm.WebApi.Watchers.TokenBalance.Rule, Ztm.Zcoin.NBitcoin.Exodus.PropertyAmount>;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public sealed class CompletedWatch
    {
        public CompletedWatch(Watch watch, int confirmation)
        {
            if (watch == null)
            {
                throw new ArgumentNullException(nameof(watch));
            }

            if (confirmation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(confirmation),
                    confirmation,
                    "The value is not a valid watch confirmation.");
            }

            Watch = watch;
            Confirmation = confirmation;
        }

        public int Confirmation { get; }

        public Watch Watch { get; }
    }
}
