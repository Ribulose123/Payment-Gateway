using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Domain.Enums
{
    public enum IdempotencyOperationType
    {
        Transfer,
        Reversal,
        WalletCredit,
        WalletDebit,
        FxExchange
    }
}
