using ABP.Core.Application.DTOs.SavingsGoal;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABP.Core.Application.Interfaces.Services
{
    public class SavingsGoalService(ISavingsGoalRepository repo, ISavingsAccountRepository accountRepo, IMapper mapper, IUnitOfWork unitOfWork) : ISavingsGoalService
    {
        private readonly ISavingsGoalRepository _repo = repo;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly IMapper _mapper = mapper;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<SavingsGoalDto> CreateAsync(CreateSavingsGoalDto dto)
        {
            var goal = new SavingsGoal
            {
                SavingsAccountId = dto.SavingsAccountId,
                Name = dto.Name,
                TargetAmount = dto.TargetAmount,
                CurrentAmount = 0,
                AutoRoundupEnabled = dto.AutoRoundupEnabled,
                ColorHex = string.IsNullOrEmpty(dto.ColorHex) ? "#C5A059" : dto.ColorHex
            };
            await _repo.AddAsync(goal);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<SavingsGoalDto>(goal);
        }

        public async Task<List<SavingsGoalDto>> GetBySavingsAccountIdAsync(int accountId)
        {
            var goals = await _repo.GetBySavingsAccountIdAsync(accountId);
            return _mapper.Map<List<SavingsGoalDto>>(goals);
        }

        public async Task<SavingsGoalDto> AddFundsAsync(int goalId, decimal amount)
        {
            var goal = await _repo.GetByIdAsync(goalId);
            if (goal != null)
            {
                var account = await _accountRepo.GetByIdAsync(goal.SavingsAccountId);
                if (account != null && account.Balance >= amount)
                {
                    account.Balance -= amount;
                    goal.CurrentAmount += amount;
                    await _repo.UpdateAsync(goal);
                    await _accountRepo.UpdateAsync(account);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            return _mapper.Map<SavingsGoalDto>(goal);
        }

        public async Task AutoRoundupAsync(string sourceAccountNumber, decimal originalAmount)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(sourceAccountNumber);
            if (account == null) return;

            var roundupTarget = Math.Ceiling(originalAmount / 100.0m) * 100;
            var diff = roundupTarget - originalAmount;

            if (diff > 0)
            {
                var goals = await _repo.GetBySavingsAccountIdAsync(account.Id);
                var activeGoal = goals.FirstOrDefault(g => g.AutoRoundupEnabled && g.CurrentAmount < g.TargetAmount);
                if (activeGoal != null && account.Balance >= diff)
                {
                    account.Balance -= diff;
                    activeGoal.CurrentAmount += diff;
                    await _repo.UpdateAsync(activeGoal);
                    await _accountRepo.UpdateAsync(account);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }
    }
}