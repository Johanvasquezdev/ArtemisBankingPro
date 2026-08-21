using ABP.Core.Domain.Entities;

namespace ABP.Core.Application.Interfaces.Services;

public interface IWebAuthnService
{
    Task RegisterCredentialAsync(UserBiometricCredential credential);
    Task<UserBiometricCredential?> GetCredentialByUserAsync(string userId);
}