namespace ABP.Core.Domain.Exceptions
{
    /// <summary>Thrown when the selected source account does not have enough funds.</summary>
    public sealed class InsufficientFundsException : DomainException
    {
        public InsufficientFundsException(string? message = null)
            : base(message ?? "No dispone del monto requerido en la cuenta seleccionada.") { }
    }

    /// <summary>Thrown when the amount to transfer exceeds the available balance of the selected account (Express transactions).</summary>
    public sealed class AmountExceedsBalanceException : DomainException
    {
        public AmountExceedsBalanceException(string? message = null)
            : base(message ?? "El monto ingresado excede el saldo disponible de la cuenta seleccionada.") { }
    }

    /// <summary>Thrown when an amount that must be greater than zero is not (cash advances).</summary>
    public sealed class CashAdvanceAmountMustBePositiveException : DomainException
    {
        public CashAdvanceAmountMustBePositiveException(string? message = null)
            : base(message ?? "El monto del avance debe ser mayor que cero.") { }
    }
}
