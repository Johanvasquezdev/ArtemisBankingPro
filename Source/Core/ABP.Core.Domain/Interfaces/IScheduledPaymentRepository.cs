using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Domain.Interfaces
{
    public interface IScheduledPaymentRepository : IGenericRepository<ScheduledPayment>
    {
        Task<List<ScheduledPayment>> GetBySavingsAccountIdAsync(int accountId);
        Task<List<ScheduledPayment>> GetActivePaymentsForDayAsync(int day);
    }
}