using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Exceptions;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Transaction = ABP.Core.Domain.Entities.Transaction;

namespace ABP.Core.Application.Interfaces.Services
{
    public class TransactionService(
        ITransactionRepository repo,
        ISavingsAccountRepository accountRepo,
        IMapper mapper,
        IUserReadOnlyService user,
        IEmailServices email,
        ICreditCardRepository creditCard,
        ILoanRepository loanRepo,
        ILoanInstallmentRepository installmentRepo,
        IBeneficiaryRepository beneficiaryRepo,
        ICreditCardConsumptionRepository consumptionRepo,
        ITransactionRecorder transactionRecorder,
        IOverpaymentCalculator overpaymentCalculator,
        ILoanPaymentAllocationService loanPaymentAllocationService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<TransactionService> logger) : ITransactionService
    {
        #region Dependencies
        private readonly ITransactionRepository _repo = repo;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly IMapper _mapper = mapper;
        private readonly IUserReadOnlyService _userService = user;
        private readonly IEmailServices _emailService = email;
        private readonly ICreditCardRepository _creditCardRepo = creditCard;
        private readonly ILoanRepository _loanRepo = loanRepo;
        private readonly ILoanInstallmentRepository _installmentRepo = installmentRepo;
        private readonly IBeneficiaryRepository _beneficiaryRepo = beneficiaryRepo;
        private readonly ICreditCardConsumptionRepository _consumptionRepo = consumptionRepo;
        private readonly ITransactionRecorder _transactionRecorder = transactionRecorder;
        private readonly IOverpaymentCalculator _overpaymentCalculator = overpaymentCalculator;
        private readonly ILoanPaymentAllocationService _loanPaymentAllocationService = loanPaymentAllocationService;
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<TransactionService> _logger = logger;
        private static readonly CultureInfo _currencyCulture = CultureInfo.GetCultureInfo("es-DO");
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

        #region Client module operations

        public async Task<CommandResult> MakeExpressTransactionAsync(MakeExpressTransactionDto dto)
        {
            var source = await GetOwnedActiveAccountAsync(dto.ClientId, dto.SourceAccountNumber);

            var destination = await _accountRepo.GetByAccountNumberAsync(dto.DestinationAccountNumber)
                ?? throw new InvalidAccountException();

            if (destination.Status != AccountStatus.Active)
                throw new InvalidAccountException();

            if (source.AccountNumber == destination.AccountNumber)
                throw new SameAccountException("La cuenta destino no puede ser la misma cuenta de origen.");

            if (source.Balance < dto.Amount)
            {
                await RecordRejectedAsync(source, destination.AccountNumber, "Transacción Express rechazada", dto.Amount);
                throw new AmountExceedsBalanceException();
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            await _accountRepo.UpdateAsync(source);
            await _accountRepo.UpdateAsync(destination);

            await _transactionRecorder.RecordDoubleEntryAsync(
                BuildTransferDebit(source, destination, "Transacción Express", dto.Amount),
                BuildTransferCredit(destination, source, "Transacción Express", dto.Amount));

            await tx.CommitAsync();

            var sourceUser = await _userService.GetByIdAsync(source.UserId);
            var destinationUser = await _userService.GetByIdAsync(destination.UserId);
            var emailOk = true;

            if (sourceUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    sourceUser.Email,
                    $"Transacción realizada a la cuenta [{LastFour(destination.AccountNumber)}]",
                    $"Se ha realizado una transacción de {FormatMoney(dto.Amount)} a la cuenta [{LastFour(destination.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            if (destinationUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    destinationUser.Email,
                    $"Transacción enviada desde la cuenta [{LastFour(source.AccountNumber)}]",
                    $"Se ha recibido una transacción de {FormatMoney(dto.Amount)} desde la cuenta [{LastFour(source.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            return CommandResult.Success(emailNotificationFailed: !emailOk);
        }

        public async Task<CommandResult> PayCreditCardAsync(PayCreditCardDto dto)
        {
            var source = await GetOwnedActiveAccountAsync(dto.ClientId, dto.SourceAccountNumber);

            var card = await _creditCardRepo.GetByCardNumberAsync(dto.CreditCardNumber)
                ?? throw new CardNotFoundException();

            if (card.ClientId != dto.ClientId)
                throw new CardNotFoundException();

            if (card.Status != CardStatus.Active)
                throw new InactiveCardException();

            if (card.AmountOwed <= 0)
            {
                await RecordRejectedAsync(source, LastFour(card.CardNumber), "Pago a tarjeta rechazado", dto.Amount);
                throw new NoOutstandingDebtException();
            }

            var effectiveAmount = _overpaymentCalculator.CalculateEffectiveAmount(dto.Amount, card.AmountOwed);

            if (source.Balance < effectiveAmount)
            {
                await RecordRejectedAsync(source, LastFour(card.CardNumber), "Pago a tarjeta rechazado", effectiveAmount);
                throw new InsufficientFundsException();
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();

            source.Balance -= effectiveAmount;
            card.AmountOwed -= effectiveAmount;
            await _accountRepo.UpdateAsync(source);
            await _creditCardRepo.UpdateAsync(card);

            var cardReference = LastFour(card.CardNumber);
            await _transactionRecorder.RecordAsync(new TransactionEntry
            {
                Amount = effectiveAmount,
                Type = TransactionType.Debit,
                Origin = source.AccountNumber,
                Beneficiary = cardReference,
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = cardReference,
                Description = "Pago a tarjeta de crédito",
                SavingAccountId = source.Id,
                Status = TransactionStatus.Approved
            });

            await tx.CommitAsync();

            var cardOwner = await _userService.GetByIdAsync(card.ClientId);
            var emailOk = true;
            if (cardOwner != null)
            {
                emailOk = await SendEmailSafeAsync(
                    cardOwner.Email,
                    $"Pago realizado a la tarjeta [{cardReference}]",
                    $"Se ha realizado un pago de {FormatMoney(effectiveAmount)} a la tarjeta [{cardReference}] desde la cuenta [{LastFour(source.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            return CommandResult.Success(emailNotificationFailed: !emailOk);
        }

        public async Task<CommandResult> PayLoanAsync(PayLoanDto dto)
        {
            var source = await GetOwnedActiveAccountAsync(dto.ClientId, dto.SourceAccountNumber);

            var loan = await _loanRepo.GetByLoanNumberAsync(dto.LoanNumber)
                ?? throw new LoanNotFoundException();

            if (loan.ClientId != dto.ClientId)
                throw new LoanNotFoundException();

            if (loan.Status != LoanStatus.Active)
                throw new LoanNotFoundException();

            var pendingInstallments = (await _installmentRepo.GetByLoanIdAsync(loan.Id))
                .Where(i => i.Status != InstallmentStatus.Paid)
                .ToList();

            if (pendingInstallments.Count == 0)
            {
                await RecordRejectedAsync(source, loan.LoanNumber, "Pago a préstamo rechazado", dto.Amount);
                throw new NoPendingInstallmentsException();
            }

            var totalPending = pendingInstallments.Sum(i => i.InstallmentAmount - i.AmountPaid);
            var effectiveAmount = _overpaymentCalculator.CalculateEffectiveAmount(dto.Amount, totalPending);

            if (source.Balance < effectiveAmount)
            {
                await RecordRejectedAsync(source, loan.LoanNumber, "Pago a préstamo rechazado", effectiveAmount);
                throw new InsufficientFundsException();
            }

            var allocation = _loanPaymentAllocationService.Allocate(pendingInstallments, effectiveAmount);

            await using var tx = await _unitOfWork.BeginTransactionAsync();

            foreach (var allocationItem in allocation.Allocations)
            {
                var installment = pendingInstallments.First(i => i.Id == allocationItem.InstallmentId);
                installment.AmountPaid += allocationItem.AppliedAmount;
                if (allocationItem.BecomesPaid)
                {
                    installment.Status = InstallmentStatus.Paid;
                    installment.IsOverdue = false;
                }
                await _installmentRepo.UpdateAsync(installment);
            }

            source.Balance -= allocation.TotalApplied;
            await _accountRepo.UpdateAsync(source);

            if (allocation.LoanFullyPaid)
            {
                loan.Status = LoanStatus.Completed;
                await _loanRepo.UpdateAsync(loan);
            }

            await _transactionRecorder.RecordAsync(new TransactionEntry
            {
                Amount = allocation.TotalApplied,
                Type = TransactionType.Debit,
                Origin = source.AccountNumber,
                Beneficiary = loan.LoanNumber,
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = loan.LoanNumber,
                Description = "Pago a préstamo",
                SavingAccountId = source.Id,
                Status = TransactionStatus.Approved
            });

            await tx.CommitAsync();

            var loanOwner = await _userService.GetByIdAsync(loan.ClientId);
            var emailOk = true;
            if (loanOwner != null)
            {
                emailOk = await SendEmailSafeAsync(
                    loanOwner.Email,
                    $"Pago realizado al préstamo [{loan.LoanNumber}]",
                    $"Se ha realizado un pago de {FormatMoney(allocation.TotalApplied)} al préstamo [{loan.LoanNumber}] desde la cuenta [{LastFour(source.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            return CommandResult.Success(emailNotificationFailed: !emailOk);
        }

        public async Task<CommandResult> PayBeneficiaryAsync(PayBeneficiaryDto dto)
        {
            var source = await GetOwnedActiveAccountAsync(dto.ClientId, dto.SourceAccountNumber);

            var beneficiary = await _beneficiaryRepo.GetByIdAsync(dto.BeneficiaryId)
                ?? throw new BeneficiaryNotFoundException();

            if (beneficiary.OwnerId != dto.ClientId)
                throw new BeneficiaryNotFoundException();

            var destination = await _accountRepo.GetByAccountNumberAsync(beneficiary.AccountNumber)
                ?? throw new InvalidAccountException();

            if (destination.Status != AccountStatus.Active)
                throw new InvalidAccountException();

            if (source.AccountNumber == destination.AccountNumber)
                throw new SameAccountException("La cuenta de origen y la cuenta de destino no pueden ser la misma.");

            if (source.Balance < dto.Amount)
            {
                await RecordRejectedAsync(source, destination.AccountNumber, "Transacción a beneficiario rechazada", dto.Amount);
                throw new InsufficientFundsException();
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            await _accountRepo.UpdateAsync(source);
            await _accountRepo.UpdateAsync(destination);

            await _transactionRecorder.RecordDoubleEntryAsync(
                BuildTransferDebit(source, destination, "Transacción a beneficiario", dto.Amount),
                BuildTransferCredit(destination, source, "Transacción a beneficiario", dto.Amount));

            await tx.CommitAsync();

            var sourceUser = await _userService.GetByIdAsync(source.UserId);
            var destinationUser = await _userService.GetByIdAsync(destination.UserId);
            var emailOk = true;

            if (sourceUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    sourceUser.Email,
                    $"Transacción realizada a la cuenta [{LastFour(destination.AccountNumber)}]",
                    $"Se ha realizado una transacción de {FormatMoney(dto.Amount)} a la cuenta [{LastFour(destination.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            if (destinationUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    destinationUser.Email,
                    $"Transacción enviada desde la cuenta [{LastFour(source.AccountNumber)}]",
                    $"Se ha recibido una transacción de {FormatMoney(dto.Amount)} desde la cuenta [{LastFour(source.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            return CommandResult.Success(emailNotificationFailed: !emailOk);
        }

        public async Task<CommandResult> TransferOwnAccountsAsync(TransferOwnAccountsDto dto)
        {
            var activeAccounts = (await _accountRepo.GetActiveAccountsByClientIdAsync(dto.ClientId)).ToList();
            if (activeAccounts.Count < 2)
                throw new InsufficientAccountsException();

            var source = activeAccounts.FirstOrDefault(a => a.AccountNumber == dto.SourceAccountNumber)
                ?? throw new InactiveAccountException();

            var destination = activeAccounts.FirstOrDefault(a => a.AccountNumber == dto.DestinationAccountNumber)
                ?? throw new InactiveAccountException();

            if (source.AccountNumber == destination.AccountNumber)
                throw new SameAccountException("La cuenta de origen y la cuenta de destino no pueden ser la misma.");

            if (source.Balance < dto.Amount)
            {
                await RecordRejectedAsync(source, destination.AccountNumber, "Transferencia entre cuentas rechazada", dto.Amount);
                throw new InsufficientFundsException();
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            await _accountRepo.UpdateAsync(source);
            await _accountRepo.UpdateAsync(destination);

            await _transactionRecorder.RecordDoubleEntryAsync(
                BuildTransferDebit(source, destination, "Transferencia entre cuentas", dto.Amount),
                BuildTransferCredit(destination, source, "Transferencia entre cuentas", dto.Amount));

            await tx.CommitAsync();

            var emailOk = await SendEmailSafeAsync(
                (await _userService.GetByIdAsync(source.UserId))?.Email ?? string.Empty,
                "Transferencia entre cuentas realizada",
                $"Se ha realizado una transferencia de {FormatMoney(dto.Amount)} entre la cuenta [{LastFour(source.AccountNumber)}] y la cuenta [{LastFour(destination.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");

            return CommandResult.Success(emailNotificationFailed: !emailOk);
        }

        public async Task<CommandResult> CashAdvanceAsync(CashAdvanceDto dto)
        {
            if (dto.Amount <= 0)
                throw new CashAdvanceAmountMustBePositiveException();

            var card = await _creditCardRepo.GetByIdAsync(dto.CreditCardId)
                ?? throw new CardNotFoundException();

            if (card.ClientId != dto.ClientId)
                throw new CardNotFoundException();

            if (card.Status != CardStatus.Active)
                throw new InactiveCardException();

            if (IsCardExpired(card.ExpirationDate))
                throw new ExpiredCardException();

            var account = await _accountRepo.GetByIdAsync(dto.SavingsAccountId)
                ?? throw new InactiveAccountException();

            if (account.UserId != dto.ClientId || account.Status != AccountStatus.Active)
                throw new InactiveAccountException();

            var availableCredit = card.CreditLimit - card.AmountOwed;
            var interest = dto.Amount * 0.0625m;
            var totalToCharge = dto.Amount + interest;

            if (totalToCharge > availableCredit)
                throw new InsufficientAvailableCreditException();

            await using var tx = await _unitOfWork.BeginTransactionAsync();

            account.Balance += dto.Amount;
            await _accountRepo.UpdateAsync(account);

            card.AmountOwed += totalToCharge;
            await _creditCardRepo.UpdateAsync(card);

            await _consumptionRepo.AddAsync(new CreditCardConsumption
            {
                Amount = totalToCharge,
                TransactionDate = _dateTimeProvider.UtcNow,
                CommerceName = "AVANCE",
                Status = ConsumptionStatus.Approved,
                CreditCardId = card.Id,
                CommerceId = null
            });

            var cardReference = LastFour(card.CardNumber);
            await _transactionRecorder.RecordAsync(new TransactionEntry
            {
                Amount = dto.Amount,
                Type = TransactionType.Credit,
                Origin = cardReference,
                Beneficiary = account.AccountNumber,
                SourceAccountNumber = cardReference,
                DestinationAccountNumber = account.AccountNumber,
                Description = "Avance de efectivo",
                SavingAccountId = account.Id,
                Status = TransactionStatus.Approved
            });

            await tx.CommitAsync();

            var cardOwner = await _userService.GetByIdAsync(card.ClientId);
            var emailOk = true;
            if (cardOwner != null)
            {
                emailOk = await SendEmailSafeAsync(
                    cardOwner.Email,
                    $"Avance de efectivo desde la tarjeta [{cardReference}]",
                    $"Se ha realizado un avance de efectivo de {FormatMoney(dto.Amount)} desde la tarjeta [{cardReference}] a la cuenta [{LastFour(account.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            return CommandResult.Success(emailNotificationFailed: !emailOk);
        }

        #endregion

        #region Cashier operations (unchanged behavior)

        public async Task<int> GetTodayTransactionsCountAsync()
            => await _repo.GetTodayTransactionsCountAsync();

        public async Task<int> GetTotalTransactionsCountAsync()
            => await _repo.GetTotalTransactionsCountAsync();

        public async Task<int> GetTodayPaymentsCountAsync()
            => await _repo.GetTodayPaymentsCountAsync();

        public async Task<int> GetTotalPaymentsCountAsync()
            => await _repo.GetTotalPaymentsCountAsync();

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

        #endregion

        #region Private helpers

        private async Task<SavingsAccount> GetOwnedActiveAccountAsync(string clientId, string accountNumber)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(accountNumber)
                ?? throw new InactiveAccountException();

            if (account.UserId != clientId || account.Status != AccountStatus.Active)
                throw new InactiveAccountException();

            return account;
        }

        private TransactionEntry BuildTransferDebit(SavingsAccount source, SavingsAccount destination, string description, decimal amount) => new()
        {
            Amount = amount,
            Type = TransactionType.Debit,
            Origin = source.AccountNumber,
            Beneficiary = destination.AccountNumber,
            SourceAccountNumber = source.AccountNumber,
            DestinationAccountNumber = destination.AccountNumber,
            Description = description,
            SavingAccountId = source.Id,
            Status = TransactionStatus.Approved
        };

        private TransactionEntry BuildTransferCredit(SavingsAccount destination, SavingsAccount source, string description, decimal amount) => new()
        {
            Amount = amount,
            Type = TransactionType.Credit,
            Origin = source.AccountNumber,
            Beneficiary = destination.AccountNumber,
            SourceAccountNumber = source.AccountNumber,
            DestinationAccountNumber = destination.AccountNumber,
            Description = description,
            SavingAccountId = destination.Id,
            Status = TransactionStatus.Approved
        };

        private async Task RecordRejectedAsync(SavingsAccount source, string reference, string description, decimal amount)
        {
            await _transactionRecorder.RecordAsync(new TransactionEntry
            {
                Amount = amount,
                Type = TransactionType.Debit,
                Origin = source.AccountNumber,
                Beneficiary = reference,
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = reference,
                Description = description,
                SavingAccountId = source.Id,
                Status = TransactionStatus.Declined
            });
        }

        private async Task<bool> SendEmailSafeAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to)) return true;

            try
            {
                await _emailService.SendAsync(to, subject, body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
                return false;
            }
        }

        private static bool IsCardExpired(string expirationDate)
        {
            if (!DateTime.TryParseExact(expirationDate, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return false;

            var lastDayOfMonth = new DateTime(parsed.Year, parsed.Month, DateTime.DaysInMonth(parsed.Year, parsed.Month));
            return lastDayOfMonth < DateTime.UtcNow.Date;
        }

        private static string LastFour(string value)
            => value.Length <= 4 ? value : value[^4..];

        private static string FormatMoney(decimal amount)
            => amount.ToString("C2", _currencyCulture);

        private static string FormatDate(DateTime date)
            => date.ToString("dd/MM/yyyy");

        private static string FormatTime(DateTime date)
            => date.ToString("HH:mm");

        #endregion
    }
}
