namespace ABP.Core.Domain.Exceptions
{
    public sealed class CardNotFoundException : DomainException
    {
        public CardNotFoundException(string? message = null)
            : base(message ?? "La tarjeta de crédito seleccionada no existe.") { }
    }

    /// <summary>Thrown when the selected credit card is not active.</summary>
    public sealed class InactiveCardException : DomainException
    {
        public InactiveCardException(string? message = null)
            : base(message ?? "La tarjeta seleccionada no se encuentra activa.") { }
    }

    /// <summary>Thrown when the selected credit card has expired.</summary>
    public sealed class ExpiredCardException : DomainException
    {
        public ExpiredCardException(string? message = null)
            : base(message ?? "La tarjeta seleccionada se encuentra vencida.") { }
    }

    /// <summary>Thrown when trying to pay a credit card that has no outstanding debt.</summary>
    public sealed class NoOutstandingDebtException : DomainException
    {
        public NoOutstandingDebtException(string? message = null)
            : base(message ?? "La tarjeta seleccionada no tiene deuda pendiente.") { }
    }

    /// <summary>Thrown when a cash advance exceeds the available credit of the card.</summary>
    public sealed class InsufficientAvailableCreditException : DomainException
    {
        public InsufficientAvailableCreditException(string? message = null)
            : base(message ?? "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.") { }
    }
}
