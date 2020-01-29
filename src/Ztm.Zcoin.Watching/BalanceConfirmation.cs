using System;
using System.Collections.Generic;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public sealed class BalanceConfirmation<TContext, TAmount>
    {
        public BalanceConfirmation(
            uint256 block,
            BitcoinAddress address,
            IReadOnlyDictionary<BalanceWatch<TContext, TAmount>, int> watches)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

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

            Block = block;
            Address = address;
            Watches = watches;
        }

        public BitcoinAddress Address { get; }

        public uint256 Block { get; }

        public IReadOnlyDictionary<BalanceWatch<TContext, TAmount>, int> Watches { get; }
    }
}
