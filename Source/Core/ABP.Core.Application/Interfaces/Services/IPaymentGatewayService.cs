namespace ABP.Core.Application.Interfaces.Services;

public interface IPaymentGatewayService
{
    Task<string> CreatePaymentSessionAsync(decimal amount, string currency, string successUrl, string cancelUrl, string clientId, string targetAccountId);
}