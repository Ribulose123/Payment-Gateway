using Microsoft.Extensions.Options;
using PaymentGate.Application.DTO;
using PaymentGate.Application.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGate.Application.Policies
{
    public class OpenErFxService:IFxService
    {
        public readonly HttpClient _httpClient;
        public readonly FxApiSettings _baseUrl;

        public OpenErFxService(HttpClient httpClient, IOptions<FxApiSettings>options)
        {
            _httpClient = httpClient;
            _baseUrl = options.Value;
        }

        public async Task<FxQuote> QuoteAsync(string from, string to, decimal amount)
        {
            var url = $"{_baseUrl.BaseUrl}{from}";

            var response = await _httpClient.GetFromJsonAsync<FxApiRequest>(url);

            if (response == null)
                throw new Exception("FX service unavailable");

            if (!response.Rates.ContainsKey(to))
                throw new Exception("Currency not supported");

            var rate = response.Rates[to];
            var convert = Math.Round(amount * rate, 2);

            return

        }
    }
}
