using System;
using System.Collections.Generic;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public interface IExodusInterpreter
    {
        Type SupportType { get; }
        IEnumerable<BalanceChange> Interpret(ExodusTransaction transaction);
    }
}