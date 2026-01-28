using PaymentGate.Domain.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.Interface
{
    public interface IWalletService
    {
        Task<Wallet> CreateWalletAsync(Guid userid, string currency);
    }
}
