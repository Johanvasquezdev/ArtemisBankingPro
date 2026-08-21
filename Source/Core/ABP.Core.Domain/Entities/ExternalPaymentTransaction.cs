using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities;

public class ExternalPaymentTransaction
{
    public Guid Id { get; set; }
    public PaymentGatewayProvider Provider { get; set; }
    public string ExternalReferenceId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "DOP";
    public ExternalPaymentStatus Status { get; set; }
    public string TargetSavingsAccountNumber { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}