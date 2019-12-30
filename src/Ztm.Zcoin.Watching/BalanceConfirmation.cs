using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public sealed class BalanceConfirmation<TContext, TAmount>
    {
        public BalanceConfirmation(
            BitcoinAddress address,
            IEnumerable<ConfirmedBalanceChange<TContext, TAmount>> changes)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (changes == null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            if (!changes.Any())
            {
                throw new ArgumentException("The collection is empty.", nameof(changes));
            }

            Address = address;
            Changes = changes;
        }

        public BitcoinAddress Address { get; }

        public IEnumerable<ConfirmedBalanceChange<TContext, TAmount>> Changes { get; }
    }
}
