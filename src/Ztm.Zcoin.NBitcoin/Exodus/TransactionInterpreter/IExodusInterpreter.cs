using System;
using System.Collections.Generic;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public interface IExodusInterpreter
    {
        bool CanInterpret(Type type);
        IEnumerable<BalanceChange> Interpret(ExodusTransaction transaction);
    }
}