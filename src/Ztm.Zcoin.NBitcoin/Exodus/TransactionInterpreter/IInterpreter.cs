using System.Collections.Generic;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public interface IInterpreter
    {
        IEnumerable<BalanceChange> Interpret(Transaction transaction);
    }
}