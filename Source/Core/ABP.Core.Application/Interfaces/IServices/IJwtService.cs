namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IJwtService
    {
        Task<string> GenerateTokenAsync(string userId, string userName, string email, IEnumerable<string> roles, int? commerceId = null);
    }
}
