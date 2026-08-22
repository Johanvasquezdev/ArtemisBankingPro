using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Domain.Interfaces
{
    public interface ISavingsGoalRepository : IGenericRepository<SavingsGoal>
    {
        Task<List<SavingsGoal>> GetBySavingsAccountIdAsync(int accountId);
    }
}
