using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Domain.Enums;
namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IUserReadOnlyService
    {
        // User management
        Task<UserDto?> GetByIdAsync(string userId);
        Task<string?> GetUserIdByCedulaAsync(string cedula);
        Task<IEnumerable<UserDto>> GetByIdsAsync(IEnumerable<string> userIds);
        Task<bool> ExistsByCedulaAsync(string cedula, string? excludingUserId = null);
        Task<string?> GetActivationTokenAsync(string userId);

        Task<IEnumerable<UserDto>> GetActiveClientsAsync(string? cedula = null);

        Task<int> GetInactiveClientsCountAsync();
        Task<int> GetActiveClientsCountAsync();

        Task<PaginatedResult<UserDto>> GetAllAsync(int page, int pageSize = 20, UserRole? role = null);
        Task<PaginatedResult<UserDto>> GetCommerceUsersAsync(int page, int pageSize = 20);
    }
}
