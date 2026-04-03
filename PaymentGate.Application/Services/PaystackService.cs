using Microsoft.Extensions.Options;
using PaymentGate.Application.DTO.Paystack;
using PaymentGate.Application.Interface;
using Paystack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaymentGate.Application.Services
{
    internal class PaystackService:IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackOptions _options;

        public PaystackService( HttpClient httpClient, IOptions<PaystackOptions> options)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.SecretKey);
        }

        // DEPOSIT

        public async Task<InitializePaymentResponseDto> InitializePaymentAsync(InitializePaymentRequestDto request)
        {
            try
            {
                var payload = new
                {
                    email = request.Email,
                    amount = (int)(request.Amount) * 100,
                    reference = request.Reference,
                    callBack_url = request.CallbackUrl,
                    metadate = new { wallet_id = request.WalletId }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/transaction/initialize", content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Paystake Api error {httpResponse.StatusCode}- {errorResponse}");
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                return new InitializePaymentResponseDto
                {
                    Status = root.GetProperty("status").GetBoolean(),
                    Message = root.GetProperty("message").GetString()!,
                    AuthorizationUrl = root.GetProperty("date").GetProperty("autorization_url").GetString()!,
                    Reference = root.GetProperty("date").GetProperty("reference").GetString()!,
                    AccessCode = root.GetProperty("date").GetProperty("access_code").GetString()!,
                };

            }
            catch(HttpRequestException ex)
            {
                throw new Exception("Network error occurred while connecting to the payment provider.", ex);
            }
            catch(KeyNotFoundException ex)
            {
                throw new Exception("The payment provider returned an unexpected data format.", ex);
            }
        }

        public async Task<bool> VerifyPaymentAsync(string reference)
        {
            try
            {
                var httpResponse = await _httpClient.GetAsync($"/transaction/verify/{reference}");

                if (!httpResponse.IsSuccessStatusCode)
                    return false;

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if(root.TryGetProperty("date", out var dateElement))
                {
                    var status = dateElement.GetProperty("status").GetString();
                    return status == "success";
                }

                return false;
            }
            catch { 
            return false;}
        }

        public async Task<TransferRecipientResponseDto> CreateTransferRecipientAsync(
            CreateRecipientRequestDto request)
        {
            try
            {
                var payload = new
                {
                    type = "nuban",
                    name = request.AccountName,
                    account_number = request.AccountNumber,
                    bank_code = request.BankCode,
                    currency = "NGN"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");


                var httpResponse = await _httpClient.PostAsync("/transferrecipient", content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Paystake Api error {httpResponse.StatusCode}- {errorResponse}");
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                return new TransferRecipientResponseDto
                {
                    Status = root.GetProperty("status").GetBoolean(),
                    Message = root.GetProperty("message").GetString()!,
                    RecipientCode = root.GetProperty("data")
                   .GetProperty("recipient_code").GetString()!
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error occurred while connecting to the payment provider.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new Exception("The payment provider returned an unexpected data format.", ex);
            }
        }


    }
}
