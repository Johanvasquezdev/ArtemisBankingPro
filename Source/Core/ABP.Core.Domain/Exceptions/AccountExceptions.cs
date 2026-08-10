namespace ABP.Core.Domain.Exceptions
{
    /// <summary>Thrown when an account number does not match any valid, active account in the system.</summary>
    public sealed class InvalidAccountException : DomainException
    {
        public InvalidAccountException(string? message = null)
            : base(message ?? "El número de cuenta ingresado no corresponde a una cuenta válida.") { }
    }

    /// <summary>Thrown when an account has been cancelled/closed and cannot be used as a beneficiary.</summary>
    public sealed class ClosedAccountException : DomainException
    {
        public ClosedAccountException(string? message = null)
            : base(message ?? "No puede agregar una cuenta cancelada como beneficiario.") { }
    }

    /// <summary>Thrown when a client tries to add one of his own accounts as a beneficiary.</summary>
    public sealed class OwnAccountException : DomainException
    {
        public OwnAccountException(string? message = null)
            : base(message ?? "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.") { }
    }

    /// <summary>Thrown when an account is already registered as a beneficiary of the client.</summary>
    public sealed class DuplicateBeneficiaryException : DomainException
    {
        public DuplicateBeneficiaryException(string? message = null)
            : base(message ?? "Esta cuenta ya se encuentra registrada como beneficiario.") { }
    }

    /// <summary>Thrown when source and destination accounts are the same account.</summary>
    public sealed class SameAccountException : DomainException
    {
        public SameAccountException(string message) : base(message) { }
    }

    /// <summary>Thrown when the selected account is not active.</summary>
    public sealed class InactiveAccountException : DomainException
    {
        public InactiveAccountException(string? message = null)
            : base(message ?? "La cuenta de ahorro seleccionada no se encuentra activa.") { }
    }

    /// <summary>Thrown when a client needs at least two active savings accounts to perform an operation.</summary>
    public sealed class InsufficientAccountsException : DomainException
    {
        public InsufficientAccountsException(string? message = null)
            : base(message ?? "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.") { }
    }
}
