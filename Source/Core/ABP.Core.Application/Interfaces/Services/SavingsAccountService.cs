using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using AutoMapper;

namespace ABP.Core.Application.Interfaces.Services
{
    public class SavingsAccountService(ISavingsAccountRepository repo, IMapper mapper, IUserReadOnlyService user, ITransactionRepository transrepo, IUnitOfWork unitOfWork) : ISavingsAccountService
    {
        private readonly ISavingsAccountRepository _repo = repo;
        private readonly IMapper _mapper = mapper;
        private readonly IUserReadOnlyService _userService = user;
        private readonly ITransactionRepository _transrepo = transrepo;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<SavingsAccountDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<SavingsAccountDto>(entity);
        }

        public async Task<SavingsAccountDto?> GetByAccountNumberAsync(string accountNumber)
        {
            var entity = await _repo.GetByAccountNumberAsync(accountNumber);
            return entity is null ? null : _mapper.Map<SavingsAccountDto>(entity);
        }

        public async Task<IEnumerable<SavingsAccountDto>> GetByClientIdAsync(string clientId)
        {
            var entities = await _repo.GetAllAccountByClienteIdAsync(clientId);
            return _mapper.Map<IEnumerable<SavingsAccountDto>>(entities);
        }

        public async Task<SavingsAccountDto?> GetPrimaryAccountByClientIdAsync(string clientId)
        {
            var entity = await _repo.GetPrimaryAccountByClientIdAsync(clientId);
            return entity is null ? null : _mapper.Map<SavingsAccountDto>(entity);
        }

        public async Task<PaginatedResult<SavingsAccountDto>> GetAllPagedAsync(int page, int pageSize = 20, AccountStatus? status = null, AccountType? type = null, string? cedula = null)
        {
            var entities = await _repo.GetAllPagedAsync(page, pageSize, status, type);
            if (!string.IsNullOrWhiteSpace(cedula))
            {
                var clientId = await _userService.GetUserIdByCedulaAsync(cedula);
                entities = clientId is null
                    ? []
                    : entities.Where(account => account.UserId == clientId);
            }
            var items = _mapper.Map<IEnumerable<SavingsAccountDto>>(entities);
            var usersById = (await _userService.GetByIdsAsync(items.Select(item => item.UserId)))
                .ToDictionary(user => user.Id);

            foreach (var item in items)
            {
                usersById.TryGetValue(item.UserId, out var user);
                if (user != null)
                    item.OwnerFullName = $"{user.FirstName} {user.LastName}";
            }

            var totalCount = await _repo.GetTotalActiveAccountsCountAsync();

            return new PaginatedResult<SavingsAccountDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<SavingsAccountDto> CreateAccountAsync(string clientId, string adminId, decimal initialAmount, AccountType type = AccountType.Primary)
        {
            string accountNumber;
            do
            {
                accountNumber = GenerateAccountNumber();
            }
            while (await _repo.AccountOrLoanNumberExistsAsync(accountNumber));

            var account = new SavingsAccount
            {
                AccountNumber = accountNumber,
                Balance = initialAmount,
                Type = type,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UserId = clientId,
                CreatedByAdminId = adminId
            };

            await using var assignmentTransaction = await _unitOfWork.BeginTransactionAsync();
            await _repo.AddWithoutSaveAsync(account);

            if (initialAmount > 0)
            {
                var transaction = new Transaction
                {
                    Amount = initialAmount,
                    Type = TransactionType.Credit,
                    TransactionDate = DateTime.UtcNow,
                    Origin = "SYSTEM",
                    Beneficiary = accountNumber,
                    Status = TransactionStatus.Approved,
                    SavingAccountId = account.Id,
                    SavingsAccount = account,
                    SourceAccountNumber = "SYSTEM",
                    DestinationAccountNumber = accountNumber,
                    Description = "Depósito inicial - apertura de cuenta",
                    CreatedAt = DateTime.UtcNow
                };

                await _transrepo.AddWithoutSaveAsync(transaction);
            }

            await _unitOfWork.SaveChangesAsync();
            await assignmentTransaction.CommitAsync();

            return _mapper.Map<SavingsAccountDto>(account);
        }

        public async Task UpdateAsync(SavingsAccountDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return;

            _mapper.Map(dto, entity);
            await _repo.UpdateWithoutSaveAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateWithoutSaveAsync(SavingsAccountDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return;

            _mapper.Map(dto, entity);
            await _repo.UpdateWithoutSaveAsync(entity);
        }

        public async Task<bool> ChangeStatusAsync(int accountId, AccountStatus status)
        {
            var entity = await _repo.GetByIdAsync(accountId);
            if (entity == null) return false;

            entity.Status = status;
            await _repo.UpdateWithoutSaveAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DepositAsync(string accountNumber, decimal amount)
        {
            if (amount <= 0) return false;

            var account = await _repo.GetByAccountNumberAsync(accountNumber);
            if (account == null || account.Status != AccountStatus.Active) return false;

            await using var depositTransaction = await _unitOfWork.BeginTransactionAsync();

            account.Balance += amount;
            await _repo.UpdateWithoutSaveAsync(account);

            var transaction = new Transaction
            {
                Amount = amount,
                Type = TransactionType.Credit,
                TransactionDate = DateTime.UtcNow,
                Origin = "SYSTEM",
                Beneficiary = accountNumber,
                Status = TransactionStatus.Approved,
                SavingAccountId = account.Id,
                SavingsAccount = account,
                SourceAccountNumber = "SYSTEM",
                DestinationAccountNumber = accountNumber,
                Description = "Depósito de saldo adicional",
                CreatedAt = DateTime.UtcNow
            };

            await _transrepo.AddWithoutSaveAsync(transaction);

            await _unitOfWork.SaveChangesAsync();
            await depositTransaction.CommitAsync();

            return true;
        }

        public async Task<bool> WithdrawAsync(string accountNumber, decimal amount)
        {
            if (amount <= 0) return false;

            var account = await _repo.GetByAccountNumberAsync(accountNumber);
            if (account == null || account.Status != AccountStatus.Active) return false;
            if (account.Balance < amount) return false;

            account.Balance -= amount;
            await _repo.UpdateWithoutSaveAsync(account);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TransferAsync(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
        {
            if (amount <= 0) return false;

            var source = await _repo.GetByAccountNumberAsync(sourceAccountNumber);
            if (source == null || source.Status != AccountStatus.Active) return false;
            if (source.Balance < amount) return false;

            var destination = await _repo.GetByAccountNumberAsync(destinationAccountNumber);
            if (destination == null || destination.Status != AccountStatus.Active) return false;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            source.Balance -= amount;
            destination.Balance += amount;

            await _repo.UpdateWithoutSaveAsync(source);
            await _repo.UpdateWithoutSaveAsync(destination);

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }



        public async Task<bool> AccountNumberExistsAsync(string accountNumber)
        {
            return await _repo.AccountOrLoanNumberExistsAsync(accountNumber);
        }

        public async Task<int> GetTotalActiveAccountsCountAsync()
        {
            return await _repo.GetTotalActiveAccountsCountAsync();
        }

        public async Task<bool> HasActiveAccountAsync(string clientId)
        {
            var accounts = await _repo.GetAllAccountByClienteIdAsync(clientId);
            return accounts.Any(a => a.Status == AccountStatus.Active);
        }

        private static string GenerateAccountNumber()
        {
            return Random.Shared.Next(100000000, 999999999).ToString();
        }

        public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(string accountNumber)
        {
            var transactions = await _transrepo.GetByAccountNumberAsync(accountNumber);

            var accountTransactions = transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t =>
                {
                    var isOutgoing = t.SourceAccountNumber == accountNumber;

                    return new TransactionDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        TransactionDate = t.TransactionDate == default ? t.CreatedAt : t.TransactionDate,
                        Type = isOutgoing ? TransactionType.Debit : TransactionType.Credit,
                        Beneficiary = isOutgoing ? t.DestinationAccountNumber : t.SourceAccountNumber,
                        Origin = isOutgoing ? t.SourceAccountNumber : t.DestinationAccountNumber,
                        Status = t.Status,
                        SavingAccountId = t.SavingAccountId,
                        CreatedAt = t.CreatedAt,
                        Description = t.Description
                    };
                })
                .ToList();

            return accountTransactions;
        }

        public async Task AssignSecondaryAsync(AssignSavingsAccountDto dto)
        {
            string accountNumber;
            do
            {
                accountNumber = Random.Shared.Next(100000000, 999999999).ToString();
            }
            while (await _repo.GetByAccountNumberAsync(accountNumber) != null);

            var account = new SavingsAccount
            {
                AccountNumber = accountNumber,
                Balance = dto.InitialBalance,
                UserId = dto.ClientId,
                CreatedByAdminId = dto.AdminId,
                Type = AccountType.Secondary,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await using var assignmentTransaction = await _unitOfWork.BeginTransactionAsync();
            await _repo.AddWithoutSaveAsync(account);

            if (dto.InitialBalance > 0)
            {
                var transaction = new Transaction
                {
                    Amount = dto.InitialBalance,
                    Type = TransactionType.Credit,
                    TransactionDate = DateTime.UtcNow,
                    Origin = "SYSTEM",
                    Beneficiary = accountNumber,
                    Status = TransactionStatus.Approved,
                    SavingAccountId = account.Id,
                    SavingsAccount = account,
                    SourceAccountNumber = "SYSTEM",
                    DestinationAccountNumber = accountNumber,
                    Description = "Depósito inicial - apertura de cuenta secundaria",
                    CreatedAt = DateTime.UtcNow
                };

                await _transrepo.AddWithoutSaveAsync(transaction);
            }

            await _unitOfWork.SaveChangesAsync();
            await assignmentTransaction.CommitAsync();
        }

        public async Task CancelAsync(string accountNumber)
        {
            var secondaryAccount = await _repo.GetByAccountNumberAsync(accountNumber);
            if (secondaryAccount == null) throw new Exception("Account not found.");

            if (secondaryAccount.Type == AccountType.Primary)
                throw new InvalidOperationException("The primary account cannot be cancelled.");
            var primaryAccount = await _repo.GetPrimaryAccountByClientIdAsync(secondaryAccount.UserId);
            if (primaryAccount == null)
                throw new Exception("Destination primary account not found.");

            await using var cancellationTransaction = await _unitOfWork.BeginTransactionAsync();

            if (secondaryAccount.Balance > 0)
            {
                decimal balanceToTransfer = secondaryAccount.Balance;

                primaryAccount.Balance += balanceToTransfer;
                secondaryAccount.Balance = 0;

                var transferDebit = new Transaction
                {
                    Amount = balanceToTransfer,
                    Type = TransactionType.Debit,
                    TransactionDate = DateTime.UtcNow,
                    SourceAccountNumber = secondaryAccount.AccountNumber,
                    DestinationAccountNumber = primaryAccount.AccountNumber,
                    Origin = secondaryAccount.AccountNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.Approved,
                    SavingAccountId = secondaryAccount.Id,
                    Description = "Cierre de cuenta secundaria - saldo transferido a la principal",
                    CreatedAt = DateTime.UtcNow
                };

                var transferCredit = new Transaction
                {
                    Amount = balanceToTransfer,
                    Type = TransactionType.Credit,
                    TransactionDate = DateTime.UtcNow,
                    SourceAccountNumber = secondaryAccount.AccountNumber,
                    DestinationAccountNumber = primaryAccount.AccountNumber,
                    Origin = secondaryAccount.AccountNumber,
                    Beneficiary = primaryAccount.AccountNumber,
                    Status = TransactionStatus.Approved,
                    SavingAccountId = primaryAccount.Id,
                    Description = "Crédito por cierre de cuenta secundaria",
                    CreatedAt = DateTime.UtcNow
                };

                await _transrepo.AddWithoutSaveAsync(transferDebit);
                await _transrepo.AddWithoutSaveAsync(transferCredit);
                await _repo.UpdateWithoutSaveAsync(primaryAccount);
            }
            secondaryAccount.Status = AccountStatus.Closed;
            await _repo.UpdateWithoutSaveAsync(secondaryAccount);
            await _unitOfWork.SaveChangesAsync();
            await cancellationTransaction.CommitAsync();
        }

    }
}
