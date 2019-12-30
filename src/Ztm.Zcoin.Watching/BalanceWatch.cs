using System;
using NBitcoin;

namespace Ztm.Zcoin.Watching
{
    public sealed class BalanceWatch<TContext, TAmount> : Watch<TContext>
    {
        public BalanceWatch(
            TContext context,
            uint256 startBlock,
            uint256 tx,
            BitcoinAddress address,
            TAmount balanceChange) : base(context, startBlock)
        {
            if (tx == null)
            {
                throw new ArgumentNullException(nameof(tx));
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (balanceChange == null)
            {
                throw new ArgumentNullException(nameof(balanceChange));
            }

            Transaction = tx;
            Address = address;
            BalanceChange = balanceChange;
        }

        public BalanceWatch(
            TContext context,
            uint256 startBlock,
            uint256 tx,
            BitcoinAddress address,
            TAmount balanceChange,
            DateTime startTime) : base(context, startBlock, startTime)
        {
            if (tx == null)
            {
                throw new ArgumentNullException(nameof(tx));
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (balanceChange == null)
            {
                throw new ArgumentNullException(nameof(balanceChange));
            }

            Transaction = tx;
            Address = address;
            BalanceChange = balanceChange;
        }

        public BalanceWatch(
            TContext context,
            uint256 startBlock,
            uint256 tx,
            BitcoinAddress address,
            TAmount balanceChange,
            DateTime startTime,
            Guid id) : base(context, startBlock, startTime, id)
        {
            if (tx == null)
            {
                throw new ArgumentNullException(nameof(tx));
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (balanceChange == null)
            {
                throw new ArgumentNullException(nameof(balanceChange));
            }

            Transaction = tx;
            Address = address;
            BalanceChange = balanceChange;
        }

        public BitcoinAddress Address { get; }

        public TAmount BalanceChange { get; }

        public uint256 Transaction { get; }
    }
}
