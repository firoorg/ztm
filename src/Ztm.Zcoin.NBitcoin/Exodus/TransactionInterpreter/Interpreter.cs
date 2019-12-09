using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Exodus.TransactionInterpreter
{
    public class Interpreter : IInterpreter
    {
        readonly IEnumerable<IExodusInterpreter> transactionInterpreter;

        public Interpreter(IEnumerable<IExodusInterpreter> transactionInterpreters)
        {
            if (transactionInterpreters == null)
            {
                throw new ArgumentNullException(nameof(transactionInterpreters));
            }

            this.transactionInterpreter = transactionInterpreters;
        }

        public IEnumerable<BalanceChange> Interpret(Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var ex = transaction.GetExodusTransaction();
            if (ex == null)
            {
                throw new ArgumentException("The transaction does not contain exodus data.", nameof(transaction));
            }

            var interpreter = this.transactionInterpreter.FirstOrDefault(i => i.CanInterpret(ex.GetType()));

            if (interpreter == null)
            {
                throw new TransactionFieldException(
                    TransactionFieldException.TypeField, "The value is unknown transaction type.");
            }

            return interpreter.Interpret(ex);
        }
    }
}