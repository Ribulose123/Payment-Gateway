namespace PaymentGate.Application.Policies
{
    public interface ITransferLimitPolicy
    {
        Task<bool> CanTransferAsync(
            Guid walletId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken = default);
    }
}
