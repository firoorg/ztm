using System;
using System.Collections.Generic;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public sealed class BalanceConfirmation<TContext, TAmount>
    {
        public BalanceConfirmation(
            BitcoinAddress address,
            IReadOnlyDictionary<BalanceWatch<TContext, TAmount>, int> watches)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (watches == null)
            {
                throw new ArgumentNullException(nameof(watches));
            }

            if (watches.Count == 0)
            {
                throw new ArgumentException("The collection is empty.", nameof(watches));
            }

            Address = address;
            Watches = watches;
        }

        public BitcoinAddress Address { get; }

        public IReadOnlyDictionary<BalanceWatch<TContext, TAmount>, int> Watches { get; }
    }
}
