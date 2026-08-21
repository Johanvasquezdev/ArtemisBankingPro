using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.identity.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.identity.Services;

public sealed class CommerceUserDirectory(IdentityContext context) : ICommerceUserDirectory
{
    public Task<bool> HasActiveUserAsync(int commerceId, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(
            user => user.Role == UserRole.Commerce && user.CommerceId == commerceId && user.IsActive,
            cancellationToken);

    public Task<string?> GetActiveUserIdAsync(int commerceId, CancellationToken cancellationToken = default) =>
        context.Users
            .Where(user => user.Role == UserRole.Commerce && user.CommerceId == commerceId && user.IsActive && user.EmailConfirmed)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<AssociatedUserDto?> GetAssociatedUserAsync(int commerceId, CancellationToken cancellationToken = default) =>
        context.Users
            .Where(user => user.Role == UserRole.Commerce && user.CommerceId == commerceId)
            .Select(user => new AssociatedUserDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
}
