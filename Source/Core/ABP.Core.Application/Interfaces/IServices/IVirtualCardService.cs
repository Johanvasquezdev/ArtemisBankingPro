using ABP.Core.Application.DTOs.VirtualCard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IVirtualCardService
    {
        Task<VirtualCardDto> CreateAsync(CreateVirtualCardDto dto);
        Task<VirtualCardDto> GetByIdAsync(int id);
        Task<List<VirtualCardDto>> GetBySavingsAccountIdAsync(int accountId);
        Task ToggleFreezeAsync(int id);
    }
}
