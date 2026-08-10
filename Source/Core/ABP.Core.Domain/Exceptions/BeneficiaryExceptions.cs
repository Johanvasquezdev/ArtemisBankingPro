namespace ABP.Core.Domain.Exceptions
{
    /// <summary>Thrown when the selected beneficiary does not belong to the client or does not exist.</summary>
    public sealed class BeneficiaryNotFoundException : DomainException
    {
        public BeneficiaryNotFoundException(string? message = null)
            : base(message ?? "El beneficiario seleccionado no existe.") { }
    }
}
