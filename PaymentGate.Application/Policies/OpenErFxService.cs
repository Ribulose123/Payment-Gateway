using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using PaymentGate.Domain.ValueObjects;

namespace PaymentGate.Application.Policies
{
    public class OpenErFxService : IFxService
    {
        private readonly HttpClient _httpClient;
        private readonly FxApiSettings _settings;

        public OpenErFxService(HttpClient httpClient, IOptions<FxApiSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<FxQuote> QuoteAsync(string from, string to, decimal amount)
        {
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                return new FxQuote(1m, Math.Round(amount, 2));

            var baseUrl = (_settings.BaseUrl ?? "").TrimEnd('/');
            var fromCode = from.ToUpperInvariant();
            var toCode = to.ToUpperInvariant();

            var url = $"{baseUrl}/{fromCode}";

            var response = await _httpClient.GetFromJsonAsync<FxApiRequest>(url);

            if (response == null)
                throw new Exception("FX service unavailable");

            if (!response.Rates.TryGetValue(toCode, out var rate))
                throw new Exception($"Currency '{toCode}' not supported for base '{fromCode}'.");

            var converted = Math.Round(amount * rate, 2);
            return new FxQuote(rate, converted);
        }
    }
}