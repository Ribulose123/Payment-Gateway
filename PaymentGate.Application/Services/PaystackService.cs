using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentGate.Application.DTO.Paystack;
using PaymentGate.Application.Interface;
using Paystack;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;

namespace PaymentGate.Application.Services
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackOptions _options;
        private readonly ILogger<PaystackService>  _logger;
        public PaystackService(HttpClient httpClient, IOptions<PaystackOptions> options, ILogger<PaystackService> logger )
        {
            _options = options.Value;
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        }

        //Post request 

        private async Task<JsonElement> PostAsync(string endpoint, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Paystack GET {endpoint}", endpoint);


            var httpResponse = await _httpClient.PostAsync(endpoint, content);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception($"Paystack API error {httpResponse.StatusCode} - {error}");
            }

            var responseString= await httpResponse.Content.ReadAsStringAsync();
            return JsonDocument.Parse(responseString).RootElement;
        }

        //Get request 
        private async Task<JsonElement> GetAsync(string endpoint)
        {
            _logger.LogInformation("Paystack GET {endpoint}", endpoint);

            var httpResponse = await _httpClient.GetAsync(endpoint);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = await httpResponse.Content.ReadAsStringAsync();
                throw new Exception($"Paystack API error {httpResponse.StatusCode} - {error}");
            }

            var responseString = await httpResponse.Content.ReadAsStringAsync();
            return JsonDocument.Parse(responseString).RootElement;
        }

        // DEPOSIT

        public async Task<InitializePaymentResponseDto> InitializePaymentAsync(
            InitializePaymentRequestDto request)
        {
            try
            {
                var root = await PostAsync("transaction/initialize", new
                {
                    email = request.Email,
                    amount = (int)(request.Amount * 100), 
                    reference = request.Reference,
                    callback_url = request.CallbackUrl,  
                    metadata = new { wallet_id = request.WalletId } 
                });

               

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
                var root = await  GetAsync($"/transaction/verify/{reference}");

               

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
                var isValidBank = await ValidateBankCodeAsync(request.BankCode);

                if (!isValidBank)
                {
                    throw new ArgumentException($"Invalid bank code: {request.BankCode}. Please use a valid bank code from Paystack.");
                }

                var root = await PostAsync("transferrecipient", new
                {
                    type = "nuban",
                    name = request.AccountName,
                    account_number = request.AccountNumber,
                    bank_code = request.BankCode,
                    currency = "NGN"
                });

               

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
                var root = await PostAsync("transfer", new
                {
                    source = "balance",
                    amount = (int)(request.Amount * 100), 
                    recipient = request.RecipientCode,
                    reference = request.Reference,
                    reason = request.Reason
                });

               

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

        // Finalizing transfer 

        public async Task<FinalizeResponseDto> FinalizeTransferAsAsync(FinalizeTransferRequestDto request)
        {
            try
            {
                var root  = await PostAsync("transfer/finalize_transfer", new
                {
                    transferCode = request.TransferCode,
                    opt = request.Otp
                });

               

                return new FinalizeResponseDto
                {
                    Status = root.GetProperty("status").GetBoolean(),
                    Message = root.GetProperty("message").GetString()!,
                    TransferCode = root.GetProperty("data")
               .GetProperty("transfer_code").GetString()!,
                    Reference = root.GetProperty("data")
               .GetProperty("reference").GetString()!
                };
            }

            catch (HttpRequestException ex)
            {
                throw new Exception("Network error while connecting to payment provider.", ex);
            }
        }

        // VIRTUAL ACCOUNT

        public async Task<VirtualAccountResponseDto> CreateVirtualAccountAsync(
            CreateVirtualAccountRequestDto request)
        {
            try
            {
                var customerRoot = await PostAsync("/customer", new
                {
                    email = request.Email,
                    first_name = request.FirstName,
                    last_name = request.LastName
                });

              

                
                var customerCode = customerRoot
                    .GetProperty("data")
                    .GetProperty("customer_code").GetString()!;

               
                var virtualRoot = await PostAsync ("/dedicated_account", new
                {
                    customer = customerCode,
                    preferred_bank = "wema-bank" 
                });

               
                

                return new VirtualAccountResponseDto
                {
                    Status = virtualRoot.GetProperty("status").GetBoolean(),
                    Message = virtualRoot.GetProperty("message").GetString()!,
                    AccountNumber = virtualRoot.GetProperty("data")
                        .GetProperty("account_number").GetString()!,
                    AccountName = virtualRoot.GetProperty("data")
                        .GetProperty("account_name").GetString()!,
                    BankName = virtualRoot.GetProperty("data")
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




        // Get all banks 
        
        public async Task<List<BankDto>> GetBanksAsync()
        {
            try
            {
                var bankRoot = await GetAsync("bank");


                var banks = new List<BankDto>();

                foreach(var bank in bankRoot.GetProperty("data").EnumerateArray())
                {
                    banks.Add(new BankDto
                    {
                        Name = bank.GetProperty("name").GetString()!,
                        Code = bank.GetProperty("code").GetString()!,
                        Slug = bank.GetProperty("slug").GetString()!,
                    });
                }
                return banks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching banks from Paystack");
                throw;
            }
        }

        private async Task<bool> ValidateBankCodeAsync(string bankCode)
        {
            var Code = await GetBanksAsync();
            return Code.Any(x => x.Code == bankCode);
        }
    }
}