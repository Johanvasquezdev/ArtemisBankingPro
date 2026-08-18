namespace ABP.Core.Application.Interfaces.IServices;

/// <summary>
/// Application port for identity data needed by commerce workflows.
/// Persistence must not know about ASP.NET Identity or its DbContext.
/// </summary>
public interface ICommerceUserDirectory
{
    Task<bool> HasActiveUserAsync(int commerceId, CancellationToken cancellationToken = default);
    Task<string?> GetActiveUserIdAsync(int commerceId, CancellationToken cancellationToken = default);
}
