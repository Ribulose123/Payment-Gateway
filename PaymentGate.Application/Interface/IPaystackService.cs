using PaymentGate.Application.DTO.Paystack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.Interface
{
    public interface IPaystackService
    {
        Task<InitializePaymentResponseDto> InitializePaymentAsync(InitializePaymentRequestDto request);
         Task<bool> VerifyPaymentAsync(string reference);

        Task<TransferRecipientResponseDto> CreateTransferRecipientAsync(
            CreateRecipientRequestDto request);
        Task<PaystackTransferResponseDto> InitiateTransferAsync(
             InitiateTransferRequestDto request);
        Task<VirtualAccountResponseDto> CreateVirtualAccountAsync(
            CreateVirtualAccountRequestDto request);
    }
}
