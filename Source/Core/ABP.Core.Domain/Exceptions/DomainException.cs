namespace ABP.Core.Domain.Exceptions
{
    public abstract class DomainException(string message) : Exception(message);

    public sealed class DuplicateOperationException()
        : DomainException("Esta operación ya fue procesada. No se aplicaron fondos nuevamente.");

    public sealed class DuplicateCommerceException(string message)
        : DomainException(message);

    public sealed class CommerceNotFoundException() 
        : DomainException("El comercio no existe.");

    public sealed class InactiveCommerceException() 
        : DomainException("El comercio se encuentra inactivo.");
}
