using ABP.Core.Application.Interfaces.IServices;

namespace ABP.Core.Application.Interfaces.Services
{
    public class AntiOverpaymentCalculator : IOverpaymentCalculator
    {
        public decimal CalculateEffectiveAmount(decimal requestedAmount, decimal outstandingBalance)
            => Math.Min(requestedAmount, Math.Max(outstandingBalance, 0));
    }
}
