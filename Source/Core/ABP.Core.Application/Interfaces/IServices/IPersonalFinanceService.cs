using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IPersonalFinanceService
    {
        Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(string clientId, int month, int year);
    }
}
