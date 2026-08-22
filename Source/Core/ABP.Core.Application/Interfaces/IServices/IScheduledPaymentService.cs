using ABP.Core.Application.DTOs.ScheduledPayment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IScheduledPaymentService
    {
        Task<ScheduledPaymentDto> CreateAsync(CreateScheduledPaymentDto dto);
        Task<List<ScheduledPaymentDto>> GetBySavingsAccountIdAsync(int accountId);
        Task ToggleActiveAsync(int id);
        Task ExecuteDuePaymentsAsync(int day);
    }
}