

namespace PaymentGate.Application.DTO
{
    public class FxApiRequest
    {
        public required Dictionary<string, decimal> Rates {set; get;}
    }
}
