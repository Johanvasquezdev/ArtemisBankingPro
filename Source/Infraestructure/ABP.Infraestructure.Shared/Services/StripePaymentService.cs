using ABP.Core.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace ABP.Infraestructure.Shared.Services;

public class StripePaymentService : IPaymentGatewayService
{
    private readonly string _secretKey;

    public StripePaymentService(IConfiguration config)
    {
        _secretKey = config["Stripe:SecretKey"] ?? "mock";
        StripeConfiguration.ApiKey = _secretKey;
    }

    public async Task<string> CreatePaymentSessionAsync(decimal amount, string currency, string successUrl, string cancelUrl, string clientId, string targetAccountId)
    {
        if (_secretKey == "mock") return successUrl; // Mock bypass

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(amount * 100),
                        Currency = currency.ToLower(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Recarga de Cuenta Artemis",
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            ClientReferenceId = clientId,
            Metadata = new Dictionary<string, string>
            {
                { "TargetAccountId", targetAccountId }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }
}