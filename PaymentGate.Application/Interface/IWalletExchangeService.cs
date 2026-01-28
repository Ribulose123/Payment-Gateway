using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.Interface
{
    public interface IWalletExchangeService
    {
        Task ExchangeAsync(
            Guid userId,
            string fromCurrency,
            string toCurrency,
            decimal amount);
    }

}
