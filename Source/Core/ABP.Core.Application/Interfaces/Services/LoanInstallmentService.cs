using AutoMapper;
using ABP.Core.Application.DTOs.LoanInstallment;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;

namespace ABP.Core.Application.Interfaces.Services
{
    public class LoanInstallmentService : ILoanInstallmentService
    {
        private readonly ILoanInstallmentRepository _repo;
        private readonly IMapper _mapper;

        public LoanInstallmentService(ILoanInstallmentRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<LoanInstallmentDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<LoanInstallmentDto>(entity);
        }

        public async Task<IEnumerable<LoanInstallmentDto>> GetByLoanIdAsync(int loanId)
        {
            var entities = await _repo.GetByLoanIdAsync(loanId);
            return _mapper.Map<IEnumerable<LoanInstallmentDto>>(entities);
        }

        public async Task<IEnumerable<LoanInstallmentDto>> GetByLoanIdsAsync(IEnumerable<int> loanIds)
        {
            var entities = await _repo.GetByLoanIdsAsync(loanIds);
            return _mapper.Map<IEnumerable<LoanInstallmentDto>>(entities);
        }

        public async Task<LoanInstallmentDto?> GetFirstPendingAsync(int loanId)
        {
            var entity = await _repo.GetFirstPendingInstallmentAsync(loanId);
            return entity is null ? null : _mapper.Map<LoanInstallmentDto>(entity);
        }

        public async Task<decimal> GetPendingAmountByLoanIdAsync(int loanId)
        {
            return await _repo.GetPendingAmountByLoanIdAsync(loanId);
        }

        public async Task<int> GetPaidCountAsync(int loanId)
        {
            return await _repo.GetPaidInstallmentsCountAsync(loanId);
        }

        public async Task<bool> PayInstallmentAsync(int installmentId, decimal amount)
        {
            if (amount <= 0) return false;

            var installment = await _repo.GetByIdAsync(installmentId);
            if (installment == null || installment.Status == InstallmentStatus.Paid) return false;

            var remaining = installment.InstallmentAmount - installment.AmountPaid;
            var paymentAmount = Math.Min(amount, remaining);

            installment.AmountPaid += paymentAmount;

            if (installment.AmountPaid >= installment.InstallmentAmount)
            {
                installment.Status = InstallmentStatus.Paid;
            }

            await _repo.UpdateAsync(installment);
            return true;
        }

        public async Task<IEnumerable<LoanInstallmentDto>> GetOverdueInstallmentsAsync()
        {
            var entities = await _repo.GetOverdueInstallmentsAsync();
            return _mapper.Map<IEnumerable<LoanInstallmentDto>>(entities);
        }
    }
}
