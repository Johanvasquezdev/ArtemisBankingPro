using ABP.Core.Application.DTOs.SavingsGoal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ISavingsGoalService
    {
        Task<SavingsGoalDto> CreateAsync(CreateSavingsGoalDto dto);
        Task<List<SavingsGoalDto>> GetBySavingsAccountIdAsync(int accountId);
        Task<SavingsGoalDto> AddFundsAsync(int goalId, decimal amount);
        Task AutoRoundupAsync(string sourceAccountNumber, decimal originalAmount);
    }
}