using PaymentGate.Application.DTO;
using PaymentGate.Domain.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.Interface
{
    public interface IWithdrawalServices
    {
        public Task<WithdrawalResponseDto> WithdrawalAsync(WithdrawalRequestDto request);
    }
}
