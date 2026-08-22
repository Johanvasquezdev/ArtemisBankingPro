using ABP.Core.Application.DTOs.ScheduledPayment;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABP.Core.Application.Interfaces.Services
{
    public class ScheduledPaymentService(IScheduledPaymentRepository repo, ISavingsAccountRepository accountRepo, ITransactionRecorder transactionRecorder, IMapper mapper, IUnitOfWork unitOfWork) : IScheduledPaymentService
    {
        private readonly IScheduledPaymentRepository _repo = repo;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly ITransactionRecorder _transactionRecorder = transactionRecorder;
        private readonly IMapper _mapper = mapper;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<ScheduledPaymentDto> CreateAsync(CreateScheduledPaymentDto dto)
        {
            var payment = new ScheduledPayment
            {
                SavingsAccountId = dto.SavingsAccountId,
                ServiceName = dto.ServiceName,
                ContractNumber = dto.ContractNumber,
                Amount = dto.Amount,
                ExecutionDay = dto.ExecutionDay,
                IsActive = true
            };
            await _repo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ScheduledPaymentDto>(payment);
        }

        public async Task<List<ScheduledPaymentDto>> GetBySavingsAccountIdAsync(int accountId)
        {
            var payments = await _repo.GetBySavingsAccountIdAsync(accountId);
            return _mapper.Map<List<ScheduledPaymentDto>>(payments);
        }

        public async Task ToggleActiveAsync(int id)
        {
            var payment = await _repo.GetByIdAsync(id);
            if (payment != null)
            {
                payment.IsActive = !payment.IsActive;
                await _repo.UpdateAsync(payment);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task ExecuteDuePaymentsAsync(int day)
        {
            var payments = await _repo.GetActivePaymentsForDayAsync(day);
            foreach (var payment in payments)
            {
                var account = await _accountRepo.GetByIdAsync(payment.SavingsAccountId);
                if (account != null && account.Balance >= payment.Amount)
                {
                    account.Balance -= payment.Amount;
                    await _accountRepo.UpdateWithoutSaveAsync(account);
                    
                    var entry = new TransactionEntry
                    {
                        Amount = payment.Amount,
                        Type = TransactionType.Payment,
                        Origin = "Artemis Banking Pro",
                        Beneficiary = payment.ServiceName,
                        SourceAccountNumber = account.AccountNumber,
                        Description = $"Pago Automático: {payment.ServiceName} - {payment.ContractNumber}",
                        SavingAccountId = account.Id,
                        Status = TransactionStatus.Approved,
                        PerformedByUserId = account.UserId
                    };
                    await _transactionRecorder.RecordWithoutSaveAsync(entry);
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
