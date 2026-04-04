using Microsoft.Extensions.Options;
using PaymentGate.Application.DTO.Paystack;
using PaymentGate.Application.Interface;
using Paystack;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PaymentGate.Application.Services
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackOptions _options;

        public PaystackService(HttpClient httpClient, IOptions<PaystackOptions> options)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        }

        // DEPOSIT

        public async Task<InitializePaymentResponseDto> InitializePaymentAsync(
            InitializePaymentRequestDto request)
        {
            try
            {
                var payload = new
                {
                    email = request.Email,
                    amount = (int)(request.Amount * 100), 
                    reference = request.Reference,
                    callback_url = request.CallbackUrl,  
                    metadata = new { wallet_id = request.WalletId } 
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/transaction/initialize", content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Paystack API error {httpResponse.StatusCode} - {errorResponse}");
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                return new InitializePaymentResponseDto
                {
                    Status = root.GetProperty("status").GetBoolean(),
                    Message = root.GetProperty("message").GetString()!,
                    AuthorizationUrl = root.GetProperty("data") 
                        .GetProperty("authorization_url").GetString()!, 
                    Reference = root.GetProperty("data").GetProperty("reference").GetString()!,
                    AccessCode = root.GetProperty("data").GetProperty("access_code").GetString()!
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while connecting to payment provider.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new Exception("Payment provider returned unexpected data format.", ex);
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

                if (root.TryGetProperty("data", out var dataElement))
                {
                    var status = dataElement.GetProperty("status").GetString();
                    return status == "success";
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // WITHDRAWAL

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
                    throw new Exception($"Paystack API error {httpResponse.StatusCode} - {errorResponse}");
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
                throw new Exception("Network error while connecting to payment provider.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new Exception("Payment provider returned unexpected data format.", ex);
            }
        }

        public async Task<PaystackTransferResponseDto> InitiateTransferAsync(
            InitiateTransferRequestDto request)
        {
            try
            {
                var payload = new
                {
                    source = "balance",
                    amount = (int)(request.Amount * 100), 
                    recipient = request.RecipientCode,
                    reference = request.Reference,
                    reason = request.Reason
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/transfer", content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Paystack API error {httpResponse.StatusCode} - {errorResponse}");
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                return new PaystackTransferResponseDto
                {
                    Status = root.GetProperty("status").GetBoolean(),
                    Message = root.GetProperty("message").GetString()!,
                    TransferCode = root.GetProperty("data").GetProperty("transfer_code").GetString()!,
                    Reference = root.GetProperty("data").GetProperty("reference").GetString()!
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while connecting to payment provider.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new Exception("Payment provider returned unexpected data format.", ex);
            }
        }

        // VIRTUAL ACCOUNT

        public async Task<VirtualAccountResponseDto> CreateVirtualAccountAsync(
            CreateVirtualAccountRequestDto request)
        {
            try
            {
                var customerPayload = new
                {
                    email = request.Email,
                    first_name = request.FirstName,
                    last_name = request.LastName
                };

                var customerJson = JsonSerializer.Serialize(customerPayload);
                var customerContent = new StringContent(customerJson, Encoding.UTF8, "application/json");

                var customerResponse = await _httpClient.PostAsync("/customer", customerContent);

                if (!customerResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await customerResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Paystack API error {customerResponse.StatusCode} - {errorResponse}");
                }

                var customerString = await customerResponse.Content.ReadAsStringAsync();
                using var customerDoc = JsonDocument.Parse(customerString);

                
                var customerCode = customerDoc.RootElement
                    .GetProperty("data")
                    .GetProperty("customer_code").GetString()!;

               
                var vaPayload = new
                {
                    customer = customerCode,
                    preferred_bank = "wema-bank" 
                };

                var vaJson = JsonSerializer.Serialize(vaPayload);
                var vaContent = new StringContent(vaJson, Encoding.UTF8, "application/json");

                var vaResponse = await _httpClient.PostAsync("/dedicated_account", vaContent);

                if (!vaResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await vaResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Paystack API error {vaResponse.StatusCode} - {errorResponse}");
                }

            
                var vaResponseString = await vaResponse.Content.ReadAsStringAsync();
                using var vaDoc = JsonDocument.Parse(vaResponseString);
                var vaRoot = vaDoc.RootElement;

                return new VirtualAccountResponseDto
                {
                    Status = vaRoot.GetProperty("status").GetBoolean(),
                    Message = vaRoot.GetProperty("message").GetString()!,
                    AccountNumber = vaRoot.GetProperty("data")
                        .GetProperty("account_number").GetString()!,
                    AccountName = vaRoot.GetProperty("data")
                        .GetProperty("account_name").GetString()!,
                    BankName = vaRoot.GetProperty("data")
                        .GetProperty("bank").GetProperty("name").GetString()!
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while connecting to payment provider.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new Exception("Payment provider returned unexpected data format.", ex);
            }
        }
    }
}