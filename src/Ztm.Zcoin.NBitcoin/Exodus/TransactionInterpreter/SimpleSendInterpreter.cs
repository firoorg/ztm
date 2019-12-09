using System;
using System.Collections.Generic;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public sealed class SimpleSendInterpreter : IExodusInterpreter
    {
        public bool CanInterpret(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type == typeof(SimpleSendV0);
        }

        public IEnumerable<BalanceChange> Interpret(ExodusTransaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var simpleSend = (SimpleSendV0)transaction;

            return new BalanceChange[]
            {
                new BalanceChange(simpleSend.Sender, PropertyAmount.Negate(simpleSend.Amount), simpleSend.Property),
                new BalanceChange(simpleSend.Receiver, simpleSend.Amount, simpleSend.Property),
            };
        }
    }
}