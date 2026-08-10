namespace ABP.Core.Application.Interfaces.IServices
{
    /// <summary>
    /// Encapsulates the shared "no overpayment" rule (anti-sobrepago): the effective
    /// amount applied to a debt can never exceed the real outstanding balance.
    /// </summary>
    public interface IOverpaymentCalculator
    {
        decimal CalculateEffectiveAmount(decimal requestedAmount, decimal outstandingBalance);
    }
}
