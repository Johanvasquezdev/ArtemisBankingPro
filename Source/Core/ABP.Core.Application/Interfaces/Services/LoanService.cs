using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using AutoMapper;

namespace ABP.Core.Application.Interfaces.Services
{
    public class LoanService(ILoanRepository repo, ILoanInstallmentRepository installmentRepo, ITransactionRepository transactionrepo, 
        ISavingsAccountRepository accountRepo, IUserReadOnlyService user, IMapper mapper, IUnitOfWork? unitOfWork = null) : ILoanService
    {
        private readonly ILoanRepository _repo = repo;
        private readonly ILoanInstallmentRepository _installmentRepo = installmentRepo;
        private readonly ITransactionRepository _transactionRepo = transactionrepo;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly IUserReadOnlyService _userService = user;
        private readonly IMapper _mapper = mapper;
        private readonly IUnitOfWork? _unitOfWork = unitOfWork;

        public async Task<LoanDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            var dto = _mapper.Map<LoanDto>(entity);
            // Actualizar cuotas y pendiente
            var installments = await _installmentRepo.GetByLoanIdAsync(entity!.Id);
            dto.TotalInstallments = installments.Count();
            dto.PaidInstallments = installments.Count(i => i.Status == InstallmentStatus.Paid);
            dto.PendingAmount = installments.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.InstallmentAmount - i.AmountPaid);
            return dto;
        }

        public async Task<LoanDto?> GetByLoanNumberAsync(string loanNumber)
        {
            var entity = await _repo.GetByLoanNumberAsync(loanNumber);
            if (entity is null) return null;
            var dto = _mapper.Map<LoanDto>(entity);
            var installments = await _installmentRepo.GetByLoanIdAsync(entity.Id);
            dto.TotalInstallments = installments.Count();
            dto.PaidInstallments = installments.Count(i => i.Status == InstallmentStatus.Paid);
            dto.PendingAmount = installments.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.InstallmentAmount - i.AmountPaid);
            return dto;
        }

        public async Task<IEnumerable<LoanDto>> GetActiveByClientIdAsync(string clientId)
        {
            var entities = await _repo.GetActiveByClientIdAsync(clientId);
            var installmentsByLoan = (await _installmentRepo.GetByLoanIdsAsync(entities.Select(entity => entity.Id)))
                .GroupBy(installment => installment.LoanId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var dtos = new List<LoanDto>();
            foreach (var entity in entities)
            {
                var dto = _mapper.Map<LoanDto>(entity);
                var installments = installmentsByLoan.GetValueOrDefault(entity.Id, []);
                dto.TotalInstallments = installments.Count();
                dto.PaidInstallments = installments.Count(i => i.Status == InstallmentStatus.Paid);
                dto.PendingAmount = installments.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.InstallmentAmount - i.AmountPaid);
                dtos.Add(dto);
            }
            return dtos;
        }

        public async Task<PaginatedResult<LoanDto>> GetAllPagedAsync(int page, int pageSize = 20, LoanStatus? status = null, string? cedula = null)
        {
            var clientId = string.IsNullOrWhiteSpace(cedula)
                ? null
                : await _userService.GetUserIdByCedulaAsync(cedula);
            var entities = await _repo.GetAllPagedAsync(page, pageSize, status, clientId);
            var items = _mapper.Map<IEnumerable<LoanDto>>(entities);
            var usersById = (await _userService.GetByIdsAsync(items.Select(item => item.ClientId)))
                .ToDictionary(user => user.Id);

            foreach (var item in items)
            {
                usersById.TryGetValue(item.ClientId, out var user);
                if (user != null)
                    item.ClientFullName = $"{user.FirstName} {user.LastName}";
            }
            var totalCount = await _repo.GetTotalActiveLoansCountAsync();

            return new PaginatedResult<LoanDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task<IEnumerable<UserDto>> GetActiveClientsWithoutLoanAsync(string? cedula = null)
        {
            var allActiveClients = await _userService.GetActiveClientsAsync(cedula);

            // 2. Filtrar usando la lógica de préstamos que ya conoce este servicio
            var clientsWithActiveLoans = (await _repo.GetActiveLoanClientIdsAsync()).ToHashSet();
            return allActiveClients.Where(client => !clientsWithActiveLoans.Contains(client.Id));
        }
        public async Task<LoanDto> AssignAsync(AssignLoanDto dto)
        {
            if (await _repo.ClientHasActiveLoanAsync(dto.ClientId))
                throw new InvalidOperationException("Client already has an active loan.");

            string loanNumber;
            do
            {
                loanNumber = Random.Shared.Next(100000000, 999999999).ToString();
            }
            while (await _repo.GetByLoanNumberAsync(loanNumber) != null);

            var loan = new Loan
            {
                LoanNumber = loanNumber,
                Amount = dto.Amount,
                AnualInterestRate = dto.AnnualInterestRate,
                TermInMonths = dto.TermInMonths,
                Status = LoanStatus.Active,
                CreatedAt = DateTime.UtcNow,
                ClientId = dto.ClientId,
                AssignedByAdminId = dto.AdminId
            };

            var primaryAccount = await _accountRepo.GetPrimaryAccountByClientIdAsync(dto.ClientId)
                ?? throw new InvalidOperationException("El cliente debe tener una cuenta principal activa para desembolsar el préstamo.");

            if (_unitOfWork is null)
                throw new InvalidOperationException("La unidad de trabajo no está disponible.");

            await using var loanTransaction = await _unitOfWork.BeginTransactionAsync();
            await _repo.AddWithoutSaveAsync(loan);
            // The first flush obtains the database-generated key while the global
            // transaction remains open. Installments and disbursement still commit atomically.
            await _unitOfWork.SaveChangesAsync();
            var createdLoan = loan;

            // French amortization: fixed monthly payment
            var totalDebt = CalculateTotalLoanDebt(dto.Amount, dto.AnnualInterestRate, dto.TermInMonths);
            var fixedPayment = totalDebt / dto.TermInMonths;

            for (int i = 1; i <= dto.TermInMonths; i++)
            {
                var installment = new LoanInstallment
                {
                    DueDate = DateTime.UtcNow.AddMonths(i),
                    InstallmentAmount = Math.Round(fixedPayment, 2),
                    AmountPaid = 0,
                    Status = InstallmentStatus.Pending,
                    IsOverdue = false,
                    InstallmentNumber = i,
                    LoanId = loan.Id,
                    PrincipalPortion = Math.Round(fixedPayment - (dto.Amount * (dto.AnnualInterestRate / 100m) / 12m), 2),
                    InterestPortion = Math.Round(dto.Amount * (dto.AnnualInterestRate / 100m) / 12m, 2),
                    Loan = loan
                };

                await _installmentRepo.AddWithoutSaveAsync(installment);
            }

            // Deposit loan amount into client's primary account
            // The primary account was validated before opening the transaction.
            if (primaryAccount != null)
            {
                primaryAccount.Balance += dto.Amount;
                await _accountRepo.UpdateWithoutSaveAsync(primaryAccount);

                await _transactionRepo.AddWithoutSaveAsync(new Transaction
                {
                    Amount = dto.Amount,
                    TransactionDate = DateTime.UtcNow,
                    Type = TransactionType.Credit,
                    Origin = loan.LoanNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    SourceAccountNumber = loan.LoanNumber,
                    DestinationAccountNumber = primaryAccount.AccountNumber,
                    Description = $"Desembolso de préstamo {loan.LoanNumber}",
                    Status = TransactionStatus.Approved,
                    SavingAccountId = primaryAccount.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await loanTransaction.CommitAsync();
            return _mapper.Map<LoanDto>(createdLoan);
        }

        public async Task<bool> PayLoanInstallmentAsync(string sourceAccountNumber, string loanNumber, decimal amount)
        {
            if (amount <= 0) return false;

            var account = await _accountRepo.GetByAccountNumberAsync(sourceAccountNumber);
            if (account == null || account.Status != AccountStatus.Active) return false;
            if (account.Balance < amount) return false;

            var loan = await _repo.GetByLoanNumberAsync(loanNumber);
            if (loan == null || loan.Status != LoanStatus.Active) return false;

            var installments = (await _installmentRepo.GetByLoanIdAsync(loan.Id))
                .Where(i => i.Status != InstallmentStatus.Paid)
                .OrderBy(i => i.InstallmentNumber)
                .ToList();
            if (!installments.Any()) return false;

            if (_unitOfWork is null)
                throw new InvalidOperationException("La unidad de trabajo no está disponible.");

            await using var loanPaymentTransaction = await _unitOfWork.BeginTransactionAsync();

            decimal remainingAmount = amount;
            foreach (var installment in installments)
            {
                if (remainingAmount <= 0) break;
                var remaining = installment.InstallmentAmount - installment.AmountPaid;
                var payment = Math.Min(remainingAmount, remaining);
                installment.AmountPaid += payment;
                if (installment.AmountPaid >= installment.InstallmentAmount)
                {
                    installment.Status = InstallmentStatus.Paid;
                }
                remainingAmount -= payment;
                 await _installmentRepo.UpdateWithoutSaveAsync(installment);
            }

            var totalPaid = amount - remainingAmount;
            account.Balance -= totalPaid;
            await _accountRepo.UpdateWithoutSaveAsync(account);

            // Check if all installments are paid
            var pendingAmount = installments
                .Where(installment => installment.Status != InstallmentStatus.Paid)
                .Sum(installment => installment.InstallmentAmount - installment.AmountPaid);
            if (pendingAmount <= 0)
            {
                loan.Status = LoanStatus.Completed;
                await _repo.UpdateWithoutSaveAsync(loan);
            }
            await _unitOfWork.SaveChangesAsync();
            await loanPaymentTransaction.CommitAsync();
            return true;
        }

        public async Task<bool> ClientHasActiveLoanAsync(string clientId)
        {
            return await _repo.ClientHasActiveLoanAsync(clientId);
        }

        public async Task<decimal> GetTotalDebtByClientIdAsync(string clientId)
        {
            return await _repo.GetTotalDebtByClientIdAsync(clientId);
        }

        public async Task<decimal> GetAverageDebtAsync()
        {
            return await _repo.GetAverageDebtAsync();
        }

        public async Task<int> GetTotalActiveLoansCountAsync()
        {
            return await _repo.GetTotalActiveLoansCountAsync();
        }

        #region private helper methods
        private static decimal CalculateTotalLoanDebt(decimal amount, decimal annualRate, int months)
        {
            if (months <= 0) return amount;
            if (annualRate == 0) return amount;

            double monthlyRate = (double)annualRate / 100.0 / 12.0;
            double p = (double)amount;
            double n = months;

            double factor = Math.Pow(1 + monthlyRate, n);
            double monthlyPayment = p * (monthlyRate * factor) / (factor - 1);

            return (decimal)(monthlyPayment * n);
        }

        public async Task<(bool IsHighRisk, decimal AverageDebt, decimal CurrentDebt)> EvaluateRiskAsync(string clientId, decimal amount, decimal rate, int months)
        {
            var averageDebt = await GetAverageDebtAsync();
            var currentDebt = await GetTotalDebtByClientIdAsync(clientId);

            var totalNewLoanDebt = CalculateTotalLoanDebt(amount, rate, months);
            var newTotalDebt = currentDebt + totalNewLoanDebt;

            bool isHighRisk = currentDebt > averageDebt || newTotalDebt > averageDebt;

            return (isHighRisk, averageDebt, currentDebt);
        }

        public async Task UpdateInterestRateAsync(int loanId, decimal newAnnualInterestRate)
        {
            var loan = await _repo.GetByIdAsync(loanId);
            if (loan == null) throw new Exception("Loan not found");

            var pendingInstallments = (await _installmentRepo.GetByLoanIdAsync(loanId))
                .Where(i => i.Status != InstallmentStatus.Paid).OrderBy(i => i.InstallmentNumber).ToList();
            if (_unitOfWork is null)
                throw new InvalidOperationException("La unidad de trabajo no está disponible.");

            await using var rateTransaction = await _unitOfWork.BeginTransactionAsync();
            loan.AnualInterestRate = newAnnualInterestRate;
            await _repo.UpdateWithoutSaveAsync(loan);
            if (!pendingInstallments.Any())
            {
                await _unitOfWork.SaveChangesAsync();
                await rateTransaction.CommitAsync();
                return;
            }

            decimal remainingBalance = pendingInstallments.Sum(i => i.InstallmentAmount - i.AmountPaid);
            int remainingMonths = pendingInstallments.Count;

            double monthlyRate = (double)newAnnualInterestRate / 100 / 12;
            decimal newFixedPayment;

            if (monthlyRate == 0)
            {
                newFixedPayment = remainingBalance / remainingMonths;
            }
            else
            {
                double factor = Math.Pow(1 + monthlyRate, remainingMonths);
                double monthlyPayment = (double)remainingBalance * (monthlyRate * factor) / (factor - 1);
                newFixedPayment = (decimal)monthlyPayment;
            }

            foreach (var installment in pendingInstallments)
            {
                installment.InstallmentAmount = Math.Round(newFixedPayment, 2);
                await _installmentRepo.UpdateWithoutSaveAsync(installment);
            }
            await _unitOfWork.SaveChangesAsync();
            await rateTransaction.CommitAsync();
        }

        #endregion
    }
}
