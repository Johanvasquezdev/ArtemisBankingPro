namespace ABP.Core.Domain.Exceptions
{
    public sealed class LoanNotFoundException : DomainException
    {
        public LoanNotFoundException(string? message = null)
            : base(message ?? "El préstamo seleccionado no existe.") { }
    }

    /// <summary>Thrown when trying to pay a loan that has no pending installments.</summary>
    public sealed class NoPendingInstallmentsException : DomainException
    {
        public NoPendingInstallmentsException(string? message = null)
            : base(message ?? "El préstamo seleccionado no tiene cuotas pendientes de pago.") { }
    }
}
