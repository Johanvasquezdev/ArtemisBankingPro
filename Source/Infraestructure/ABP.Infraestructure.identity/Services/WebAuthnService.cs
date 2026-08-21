using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Entities;
using ABP.Infraestructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.identity.Services;

public class WebAuthnService : IWebAuthnService
{
    private readonly ArtemisBankingDbContext _dbContext;

    public WebAuthnService(ArtemisBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RegisterCredentialAsync(UserBiometricCredential credential)
    {
        _dbContext.BiometricCredentials.Add(credential);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<UserBiometricCredential?> GetCredentialByUserAsync(string userId)
    {
        return await _dbContext.BiometricCredentials.FirstOrDefaultAsync(c => c.UserId == userId);
    }
}