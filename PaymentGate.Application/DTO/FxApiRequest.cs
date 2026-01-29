

namespace PaymentGate.Application.DTO
{
    public class FxApiRequest
    {
        public Dictionary<string, decimal> Rates { set; get; } = new();
    }
}
