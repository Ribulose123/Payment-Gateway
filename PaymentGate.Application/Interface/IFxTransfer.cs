using PaymentGate.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.Interface
{
    public interface IFxTransfer
    {
        Task<FxTransferResponseDto> FxTransFereAsync(FxTransferRequestDto requestDto); 
    }
}
