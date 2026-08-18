using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Commerce;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ICommerceService
    {
        Task<CommerceDto?> GetByIdAsync(int id);
        Task<string?> GetActiveUserIdAsync(int commerceId);
        Task<bool> HasActiveUserAsync(int commerceId);
        Task<IEnumerable<CommerceDto>> GetAllAsync();
        Task<PaginatedResult<CommerceDto>> GetAllPagedAsync(int page, int pageSize = 20, bool? isActive = null);
        Task<CommerceDto> AddAsync(CommerceDto dto);
        Task UpdateAsync(CommerceDto dto);
        Task ChangeStatusAsync(int id, bool isActive);
        Task DeleteAsync(int id);
    }
}
