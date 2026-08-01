using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using System.Transactions;
using Transaction = ABP.Core.Domain.Entities.Transaction;
using TransactionStatus = ABP.Core.Domain.Enums.TransactionStatus;

namespace ABP.Core.Application.Interfaces.Services
{
    public class TransactionService(ITransactionRepository repo, ISavingsAccountRepository accountRepo, 
        IMapper mapper, IUserReadOnlyService user, IEmailServices email, ICreditCardRepository creditCard, ILoanRepository loanrepo, 
        ILoanInstallmentRepository installment) : ITransactionService
    {
        #region Constructor and Dependencies
        private readonly ITransactionRepository _repo = repo;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly IMapper _mapper = mapper;
        private readonly IUserReadOnlyService _userService = user;
        private readonly IEmailServices _emailService = email;
        private readonly ICreditCardRepository _creditCardRepo = creditCard;
        private readonly ILoanRepository _loanRepo = loanrepo;
        private readonly ILoanInstallmentRepository _installmentRepo = installment;
        #endregion

        public async Task<TransactionDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<TransactionDto>(entity);
        }

        public async Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int savingsAccountId)
        {
            var entities = await _repo.GetByAccountIdAsync(savingsAccountId);
            return _mapper.Map<IEnumerable<TransactionDto>>(entities);
        }

        public async Task<TransactionDto> TransferAsync(TransferDto dto)
        {
            if (dto.SourceAccountNumber == dto.DestinationAccountNumber)
                throw new InvalidOperationException("The source and destination accounts cannot be the same.");

            var source = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber)
                ?? throw new InvalidOperationException("Source account not found.");

            var destination = await _accountRepo.GetByAccountNumberAsync(dto.DestinationAccountNumber)
                ?? throw new InvalidOperationException("Destination account not found.");

            if (source.Status != AccountStatus.Active || destination.Status != AccountStatus.Active)
                throw new InvalidOperationException("Both accounts must be active.");

            if (source.Balance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds.");

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;

            await _accountRepo.UpdateAsync(source);
            await _accountRepo.UpdateAsync(destination);

            var transaction = new Transaction
            {
                Amount = dto.Amount,
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Debit,
                Origin = dto.SourceAccountNumber,
                Beneficiary = dto.DestinationAccountNumber,
                Status = TransactionStatus.Approved,
                SavingAccountId = source.Id,
                SourceAccountNumber = dto.SourceAccountNumber,
                DestinationAccountNumber = dto.DestinationAccountNumber,
                Description = "Transfer",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(transaction);
            return _mapper.Map<TransactionDto>(transaction);
        }

        public async Task<TransactionDto> PayExpressAsync(PaymentDto dto)
        {
            var source = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber)
                ?? throw new InvalidOperationException("Source account not found.");

            var destination = await _accountRepo.GetByAccountNumberAsync(dto.DestinationAccountNumber)
                ?? throw new InvalidOperationException("Destination account not found.");

            if (source.Status != AccountStatus.Active)
                throw new InvalidOperationException("Source account is not active.");

            if (source.Balance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds.");

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;

            await _accountRepo.UpdateAsync(source);
            await _accountRepo.UpdateAsync(destination);

            var transaction = new Transaction
            {
                Amount = dto.Amount,
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Debit,
                Origin = dto.SourceAccountNumber,
                Beneficiary = dto.DestinationAccountNumber,
                Status = TransactionStatus.Approved,
                SavingAccountId = source.Id,
                SourceAccountNumber = dto.SourceAccountNumber,
                DestinationAccountNumber = dto.DestinationAccountNumber,
                Description = "Express payment",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(transaction);
            return _mapper.Map<TransactionDto>(transaction);
        }

        public async Task<TransactionDto> PayCreditCardAsync(PaymentDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("The amount must be greater than zero.");

            var source = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber)
                ?? throw new InvalidOperationException("Source account not found.");

            var card = await _creditCardRepo.GetByCardNumberAsync(dto.DestinationAccountNumber)
                ?? throw new InvalidOperationException("Credit card not found.");

            if (source.Status != AccountStatus.Active)
                throw new InvalidOperationException("Source account is not active.");

            if (card.Status != CardStatus.Active)
                throw new InvalidOperationException("Credit card is not active.");

            if (card.AmountOwed <= 0)
                throw new InvalidOperationException("This card has no outstanding debt.");

            var actualPayment = Math.Min(dto.Amount, card.AmountOwed);

            if (source.Balance < actualPayment)
                throw new InvalidOperationException("Insufficient funds.");

            source.Balance -= actualPayment;
            card.AmountOwed -= actualPayment;

            await _accountRepo.UpdateAsync(source);
            await _creditCardRepo.UpdateAsync(card);

            var destinationReference = $"CARD-{card.CardNumber[^4..]}";

            var transaction = new Transaction
            {
                Amount = actualPayment,
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Credit,
                Origin = dto.SourceAccountNumber,
                Beneficiary = dto.DestinationAccountNumber,
                SourceAccountNumber = dto.SourceAccountNumber,
                DestinationAccountNumber = destinationReference,
                Description = "Credit card payment",
                Status = TransactionStatus.Approved,
                SavingAccountId = source.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(transaction);
            return _mapper.Map<TransactionDto>(transaction);
        }

        public async Task<TransactionDto> PayLoanAsync(PaymentDto dto)
        {
            var source = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber)
                ?? throw new InvalidOperationException("Source account not found.");

            if (source.Status != AccountStatus.Active)
                throw new InvalidOperationException("Source account is not active.");

            if (source.Balance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds.");

            source.Balance -= dto.Amount;
            await _accountRepo.UpdateAsync(source);

            var transaction = new Transaction
            {
                Amount = dto.Amount,
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Credit,
                Origin = dto.SourceAccountNumber,
                Beneficiary = dto.DestinationAccountNumber,
                Status = TransactionStatus.Approved,
                SavingAccountId = source.Id
            };

            await _repo.AddAsync(transaction);
            return _mapper.Map<TransactionDto>(transaction);
        }

        public async Task<int> GetTodayTransactionsCountAsync()
        {
            return await _repo.GetTodayTransactionsCountAsync();
        }

        public async Task<int> GetTotalTransactionsCountAsync()
        {
            return await _repo.GetTotalTransactionsCountAsync();
        }

        public async Task<int> GetTodayPaymentsCountAsync()
        {
            return await _repo.GetTodayPaymentsCountAsync();
        }

        public async Task<int> GetTotalPaymentsCountAsync()
        {
            return await _repo.GetTotalPaymentsCountAsync();
        }

        public async Task DepositAsync(CashierDepositDto cashierDepositDto)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(cashierDepositDto.AccountNumber);
            if (account == null)
                throw new Exception("The destination account does not exist.");

            if (account.Status != AccountStatus.Active)
                throw new InvalidOperationException("Cannot deposit into an inactive or cancelled account.");
            account.Balance += cashierDepositDto.Amount;
            await _accountRepo.UpdateAsync(account);

            var transaction = new Transaction
            {
                Amount = cashierDepositDto.Amount,
                Type = TransactionType.Credit,
                DestinationAccountNumber = cashierDepositDto.AccountNumber,
                SourceAccountNumber = "CASHIER",
                Description = "Cash deposit made at branch",
                CreatedAt = DateTime.UtcNow,
                SavingAccountId = account.Id
            };
            await _repo.AddAsync(transaction);
            var user = await _userService.GetByIdAsync(account.UserId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Deposit Received",
                        $"A deposit of {cashierDepositDto.Amount:C2} has been credited to your account {cashierDepositDto.AccountNumber}.");
                }
                catch { }
            }
        }

        public async Task WithdrawAsync(CashierWithdrawalDto dto)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(dto.AccountNumber);

            if (account == null)
                throw new Exception("The source account does not exist.");

            if (account.Status != AccountStatus.Active)
                throw new InvalidOperationException("Cannot withdraw from an inactive or cancelled account.");
            if (account.Balance < dto.Amount)
            {
                throw new InvalidOperationException($"Insufficient funds. Current balance: ${account.Balance:N2}");
            }

            account.Balance -= dto.Amount;
            await _accountRepo.UpdateAsync(account);
            var transaction = new Transaction
            {
                Amount = dto.Amount,
                Type = TransactionType.Debit,
                SourceAccountNumber = dto.AccountNumber,
                DestinationAccountNumber = "CASHIER",
                Description = "Cash withdrawal made at branch",
                CreatedAt = DateTime.UtcNow,
                SavingAccountId = account.Id
            };
            await _repo.AddAsync(transaction);

            var user = await _userService.GetByIdAsync(account.UserId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Withdrawal Notification",
                        $"A withdrawal of {dto.Amount:C2} has been processed from your account {dto.AccountNumber}.");
                }
                catch { }
            }
        }

        public async Task CashierPayCreditCardAsync(CashierPayCreditCardDto dto)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber);
            if (account == null)
                throw new Exception("The source account does not exist.");

            if (account.Status != AccountStatus.Active)
                throw new InvalidOperationException("Cannot process payment from an inactive or cancelled account.");

            var card = await _creditCardRepo.GetByCardNumberAsync(dto.CardNumber);
            if (card == null)
                throw new Exception("Credit card not found.");

            if (card.Status != CardStatus.Active)
                throw new InvalidOperationException("Cannot process payments for an inactive or cancelled card.");

            if (account.Balance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds in the source account.");

            if (card.AmountOwed <= 0)
                throw new InvalidOperationException("This card has no outstanding debt.");

            var actualPayment = Math.Min(dto.Amount, card.AmountOwed);

            account.Balance -= actualPayment;
            card.AmountOwed -= actualPayment;

            await _accountRepo.UpdateAsync(account);
            await _creditCardRepo.UpdateAsync(card);

            var destinationReference = $"CARD-{card.CardNumber[^4..]}";

            var transaction = new Transaction
            {
                Amount = actualPayment,
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Debit,
                Origin = dto.SourceAccountNumber,
                Beneficiary = dto.CardNumber,
                Status = TransactionStatus.Approved,
                SavingAccountId = account.Id,
                SourceAccountNumber = dto.SourceAccountNumber,
                DestinationAccountNumber = destinationReference,
                Description = "Credit card payment made at branch",
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(transaction);

            var user = await _userService.GetByIdAsync(card.ClientId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Credit Card Payment Received",
                        $"A payment of {actualPayment:C2} has been applied to your card ending in {card.CardNumber.Substring(card.CardNumber.Length - 4)}.");
                }
                catch { }
            }
        }

        public async Task CashierPayLoanAsync(CashierPayLoanDto Dto)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(Dto.SourceAccountNumber);
            var loan = await _loanRepo.GetByLoanNumberAsync(Dto.LoanNumber);

            if (account == null || loan == null) throw new Exception("Account or Loan not found.");
            if (account.Balance < Dto.Amount) throw new InvalidOperationException("Insufficient funds in the source account.");

            var installments = (await _installmentRepo.GetByLoanIdAsync(loan.Id))
                .Where(i => i.Status != InstallmentStatus.Paid).OrderBy(i => i.DueDate).ToList();

            decimal remainingPayment = Dto.Amount;
            decimal totalActuallyPaid = 0;
            foreach (var installment in installments)
            {
                if (remainingPayment <= 0) break;

                decimal amountNeeded = installment.InstallmentAmount - installment.AmountPaid;
                decimal paymentForThisInstallment = Math.Min(remainingPayment, amountNeeded);

                installment.AmountPaid += paymentForThisInstallment;
                remainingPayment -= paymentForThisInstallment;
                totalActuallyPaid += paymentForThisInstallment;

                if (installment.AmountPaid >= installment.InstallmentAmount)
                {
                    installment.Status = InstallmentStatus.Paid;
                }

                await _installmentRepo.UpdateAsync(installment);
            }
            account.Balance -= totalActuallyPaid;
            await _accountRepo.UpdateAsync(account);

            var stillPending = (await _installmentRepo.GetByLoanIdAsync(loan.Id))
                        .Any(i => i.Status != InstallmentStatus.Paid);

            if (!stillPending)
            {
                loan.Status = LoanStatus.Completed;
                await _loanRepo.UpdateAsync(loan);
            }
            await _repo.AddAsync(new Transaction
            {
                Amount = totalActuallyPaid,
                Type = TransactionType.Debit,
                SourceAccountNumber = Dto.SourceAccountNumber,
                DestinationAccountNumber = Dto.LoanNumber,
                Description = $"Loan payment applied to {loan.LoanNumber}",
                CreatedAt = DateTime.UtcNow,
                SavingAccountId = account.Id
            });
            var user = await _userService.GetByIdAsync(loan.ClientId);
            await _emailService.SendAsync(user.Email, "Loan Payment Applied",
                $"A payment of {totalActuallyPaid:C2} was applied to your loan {loan.LoanNumber}.");
        }

        public async Task CashierTransferAsync(CashierTransferDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("The transfer amount must be greater than zero.");

            if (dto.SourceAccountNumber == dto.DestinationAccountNumber)
                throw new InvalidOperationException("The source and destination accounts cannot be the same.");

            var sourceAccount = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber)
                ?? throw new Exception("Source account not found.");

            var destAccount = await _accountRepo.GetByAccountNumberAsync(dto.DestinationAccountNumber)
                ?? throw new Exception("Destination account not found.");

            if (sourceAccount.Status != AccountStatus.Active || destAccount.Status != AccountStatus.Active)
                throw new InvalidOperationException("Both accounts must be active.");

            if (sourceAccount.Balance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds in the source account.");

            sourceAccount.Balance -= dto.Amount;
            destAccount.Balance += dto.Amount;

            await _accountRepo.UpdateAsync(sourceAccount);
            await _accountRepo.UpdateAsync(destAccount);

            await _repo.AddAsync(new Transaction
            {
                Amount = dto.Amount,
                Type = TransactionType.Debit,
                TransactionDate = DateTime.UtcNow,
                Origin = dto.SourceAccountNumber,
                Beneficiary = dto.DestinationAccountNumber,
                Status = TransactionStatus.Approved,
                SavingAccountId = sourceAccount.Id,
                SourceAccountNumber = dto.SourceAccountNumber,
                DestinationAccountNumber = dto.DestinationAccountNumber,
                Description = $"Transfer to {dto.DestinationAccountNumber}",
                CreatedAt = DateTime.UtcNow
            });

            await _repo.AddAsync(new Transaction
            {
                Amount = dto.Amount,
                Type = TransactionType.Credit,
                TransactionDate = DateTime.UtcNow,
                Origin = dto.SourceAccountNumber,
                Beneficiary = dto.DestinationAccountNumber,
                Status = TransactionStatus.Approved,
                SavingAccountId = destAccount.Id,
                SourceAccountNumber = dto.SourceAccountNumber,
                DestinationAccountNumber = dto.DestinationAccountNumber,
                Description = $"Transfer from {dto.SourceAccountNumber}",
                CreatedAt = DateTime.UtcNow
            });

            var sourceUser = await _userService.GetByIdAsync(sourceAccount.UserId);
            var destUser = await _userService.GetByIdAsync(destAccount.UserId);

            if (sourceUser != null)
            {
                try
                {
                    await _emailService.SendAsync(sourceUser.Email, "Transfer Sent",
                        $"You have sent {dto.Amount:C2} to account {dto.DestinationAccountNumber}.");
                }
                catch { }
            }

            if (destUser != null)
            {
                try
                {
                    await _emailService.SendAsync(destUser.Email, "Transfer Received",
                        $"You have received {dto.Amount:C2} from account {dto.SourceAccountNumber}.");
                }
                catch { }
            }
        }
    }
}
