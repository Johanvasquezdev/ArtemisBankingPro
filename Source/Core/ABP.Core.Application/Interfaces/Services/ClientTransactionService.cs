using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Exceptions;
using ABP.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Transaction = ABP.Core.Domain.Entities.Transaction;

namespace ABP.Core.Application.Interfaces.Services;

internal sealed class ClientTransactionService : IClientTransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly ISavingsAccountRepository _accountRepo;
    private readonly IUserReadOnlyService _userService;
    private readonly IEmailServices _emailService;
    private readonly ICreditCardRepository _creditCardRepo;
    private readonly ILoanRepository _loanRepo;
    private readonly ILoanInstallmentRepository _installmentRepo;
    private readonly IBeneficiaryRepository _beneficiaryRepo;
    private readonly ICreditCardConsumptionRepository _consumptionRepo;
    private readonly ITransactionRecorder _transactionRecorder;
    private readonly IOverpaymentCalculator _overpaymentCalculator;
    private readonly ILoanPaymentAllocationService _loanPaymentAllocationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly IIdempotencyRepository? _idempotencyRepository;
    private static readonly CultureInfo _currencyCulture = CultureInfo.GetCultureInfo("es-DO");

    public ClientTransactionService(TransactionOperationDependencies dependencies)
    {
        _repo = dependencies.TransactionRepository;
        _accountRepo = dependencies.SavingsAccountRepository;
        _userService = dependencies.UserService;
        _emailService = dependencies.EmailService;
        _creditCardRepo = dependencies.CreditCardRepository;
        _loanRepo = dependencies.LoanRepository;
        _installmentRepo = dependencies.LoanInstallmentRepository;
        _beneficiaryRepo = dependencies.BeneficiaryRepository;
        _consumptionRepo = dependencies.ConsumptionRepository;
        _transactionRecorder = dependencies.TransactionRecorder;
        _overpaymentCalculator = dependencies.OverpaymentCalculator;
        _loanPaymentAllocationService = dependencies.LoanPaymentAllocationService;
        _dateTimeProvider = dependencies.DateTimeProvider;
        _unitOfWork = dependencies.UnitOfWork;
        _logger = dependencies.Logger;
        _idempotencyRepository = dependencies.IdempotencyRepository;
    }

        public async Task<CommandResult> MakeExpressTransactionAsync(MakeExpressTransactionDto dto)
        {
            var source = await GetOwnedActiveAccountAsync(dto.ClientId, dto.SourceAccountNumber);

            var destination = await _accountRepo.GetByAccountNumberAsync(dto.DestinationAccountNumber)
                ?? throw new InvalidAccountException();

            var destOwner = await _userService.GetByIdAsync(destination.UserId);
            if (destOwner == null || !destOwner.IsActive)
                throw new InvalidAccountException();

            if (destination.Status != AccountStatus.Active)
                throw new InvalidAccountException();

            if (source.AccountNumber == destination.AccountNumber)
                throw new SameAccountException("La cuenta destino no puede ser la misma cuenta de origen.");

            if (source.Balance < dto.Amount)
            {
                await RecordRejectedAsync(source, destination.AccountNumber, "Transaccion Express rechazada", dto.Amount);
                throw new AmountExceedsBalanceException();
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("client.express", dto.ClientId, dto.IdempotencyKey);

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            await _accountRepo.UpdateWithoutSaveAsync(source);
            await _accountRepo.UpdateWithoutSaveAsync(destination);

            await _transactionRecorder.RecordDoubleEntryWithoutSaveAsync(
                BuildTransferDebit(source, destination, "Transaccion Express", dto.Amount),
                BuildTransferCredit(destination, source, "Transaccion Express", dto.Amount));

            await _unitOfWork.SaveChangesAsync();
            await tx.CommitAsync();

            var sourceUser = await _userService.GetByIdAsync(source.UserId);
            var destinationUser = await _userService.GetByIdAsync(destination.UserId);
            var emailOk = true;

            if (sourceUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    sourceUser.Email,
                    $"Transaccion realizada a la cuenta [{LastFour(destination.AccountNumber)}]",
                    $"Se ha realizado una transaccion de {FormatMoney(dto.Amount)} a la cuenta [{LastFour(destination.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            if (destinationUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    destinationUser.Email,
                    $"Transaccion enviada desde la cuenta [{LastFour(source.AccountNumber)}]",
                    $"Se ha recibido una transaccion de {FormatMoney(dto.Amount)} desde la cuenta [{LastFour(source.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            return CommandResult.Success(emailNotificationFailed: !emailOk);
        }

        public async Task<CommandResult> PayCreditCardAsync(PayCreditCardDto dto)
        {
            var source = await GetOwnedActiveAccountAsync(dto.ClientId, dto.SourceAccountNumber);

            var card = await _creditCardRepo.GetByIdAsync(dto.CreditCardId)
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
            await ReserveIdempotencyAsync("client.card-payment", dto.ClientId, dto.IdempotencyKey);

            source.Balance -= effectiveAmount;
            card.AmountOwed -= effectiveAmount;
            await _accountRepo.UpdateWithoutSaveAsync(source);
            await _creditCardRepo.UpdateWithoutSaveAsync(card);

            var cardReference = LastFour(card.CardNumber);
            await _transactionRecorder.RecordWithoutSaveAsync(new TransactionEntry
            {
                Amount = effectiveAmount,
                Type = TransactionType.Payment,
                Origin = source.AccountNumber,
                Beneficiary = cardReference,
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = cardReference,
                Description = "Pago a tarjeta de credito",
                SavingAccountId = source.Id,
                Status = TransactionStatus.Approved,
                PerformedByUserId = source.UserId
            });

            await _unitOfWork.SaveChangesAsync();
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
                await RecordRejectedAsync(source, loan.LoanNumber, "Pago a prestamo rechazado", dto.Amount);
                throw new NoPendingInstallmentsException();
            }

            var totalPending = pendingInstallments.Sum(i => i.InstallmentAmount - i.AmountPaid);
            var effectiveAmount = _overpaymentCalculator.CalculateEffectiveAmount(dto.Amount, totalPending);

            if (source.Balance < effectiveAmount)
            {
                await RecordRejectedAsync(source, loan.LoanNumber, "Pago a prestamo rechazado", effectiveAmount);
                throw new InsufficientFundsException();
            }

            var allocation = _loanPaymentAllocationService.Allocate(pendingInstallments, effectiveAmount);

            await using var tx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("client.loan-payment", dto.ClientId, dto.IdempotencyKey);

            foreach (var allocationItem in allocation.Allocations)
            {
                var installment = pendingInstallments.First(i => i.Id == allocationItem.InstallmentId);
                installment.AmountPaid += allocationItem.AppliedAmount;
                if (allocationItem.BecomesPaid)
                {
                    installment.Status = InstallmentStatus.Paid;
                    installment.IsOverdue = false;
                }
                await _installmentRepo.UpdateWithoutSaveAsync(installment);
            }

            source.Balance -= allocation.TotalApplied;
            await _accountRepo.UpdateWithoutSaveAsync(source);

            if (allocation.LoanFullyPaid)
            {
                loan.Status = LoanStatus.Completed;
                await _loanRepo.UpdateWithoutSaveAsync(loan);
            }

            await _transactionRecorder.RecordWithoutSaveAsync(new TransactionEntry
            {
                Amount = allocation.TotalApplied,
                Type = TransactionType.Payment,
                Origin = source.AccountNumber,
                Beneficiary = loan.LoanNumber,
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = loan.LoanNumber,
                Description = "Pago a prestamo",
                SavingAccountId = source.Id,
                Status = TransactionStatus.Approved,
                PerformedByUserId = source.UserId
            });

            await _unitOfWork.SaveChangesAsync();
            await tx.CommitAsync();

            var loanOwner = await _userService.GetByIdAsync(loan.ClientId);
            var emailOk = true;
            if (loanOwner != null)
            {
                emailOk = await SendEmailSafeAsync(
                    loanOwner.Email,
                    $"Pago realizado al prestamo [{loan.LoanNumber}]",
                    $"Se ha realizado un pago de {FormatMoney(allocation.TotalApplied)} al prestamo [{loan.LoanNumber}] desde la cuenta [{LastFour(source.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
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

            var destOwner = await _userService.GetByIdAsync(destination.UserId);
            if (destOwner == null || !destOwner.IsActive)
                throw new InvalidAccountException();

            if (destination.Status != AccountStatus.Active)
                throw new InvalidAccountException();

            if (source.AccountNumber == destination.AccountNumber)
                throw new SameAccountException("La cuenta de origen y la cuenta de destino no pueden ser la misma.");

            if (source.Balance < dto.Amount)
            {
                await RecordRejectedAsync(source, destination.AccountNumber, "Transaccion a beneficiario rechazada", dto.Amount);
                throw new InsufficientFundsException();
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("client.beneficiary-payment", dto.ClientId, dto.IdempotencyKey);

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            await _accountRepo.UpdateWithoutSaveAsync(source);
            await _accountRepo.UpdateWithoutSaveAsync(destination);

            await _transactionRecorder.RecordDoubleEntryWithoutSaveAsync(
                BuildTransferDebit(source, destination, "Transaccion a beneficiario", dto.Amount),
                BuildTransferCredit(destination, source, "Transaccion a beneficiario", dto.Amount));

            await _unitOfWork.SaveChangesAsync();
            await tx.CommitAsync();

            var sourceUser = await _userService.GetByIdAsync(source.UserId);
            var destinationUser = await _userService.GetByIdAsync(destination.UserId);
            var emailOk = true;

            if (sourceUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    sourceUser.Email,
                    $"Transaccion realizada a la cuenta [{LastFour(destination.AccountNumber)}]",
                    $"Se ha realizado una transaccion de {FormatMoney(dto.Amount)} a la cuenta [{LastFour(destination.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
            }

            if (destinationUser != null)
            {
                emailOk &= await SendEmailSafeAsync(
                    destinationUser.Email,
                    $"Transaccion enviada desde la cuenta [{LastFour(source.AccountNumber)}]",
                    $"Se ha recibido una transaccion de {FormatMoney(dto.Amount)} desde la cuenta [{LastFour(source.AccountNumber)}] el {FormatDate(_dateTimeProvider.UtcNow)} a las {FormatTime(_dateTimeProvider.UtcNow)}.");
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
            await ReserveIdempotencyAsync("client.account-transfer", dto.ClientId, dto.IdempotencyKey);

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            await _accountRepo.UpdateWithoutSaveAsync(source);
            await _accountRepo.UpdateWithoutSaveAsync(destination);

            await _transactionRecorder.RecordDoubleEntryWithoutSaveAsync(
                BuildTransferDebit(source, destination, "Transferencia entre cuentas", dto.Amount),
                BuildTransferCredit(destination, source, "Transferencia entre cuentas", dto.Amount));

            await _unitOfWork.SaveChangesAsync();
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
            await ReserveIdempotencyAsync("client.cash-advance", dto.ClientId, dto.IdempotencyKey);

            account.Balance += dto.Amount;
            await _accountRepo.UpdateWithoutSaveAsync(account);

            card.AmountOwed += totalToCharge;
            await _creditCardRepo.UpdateWithoutSaveAsync(card);

            await _consumptionRepo.AddWithoutSaveAsync(new CreditCardConsumption
            {
                Amount = totalToCharge,
                TransactionDate = _dateTimeProvider.UtcNow,
                CommerceName = "AVANCE",
                Status = ConsumptionStatus.Approved,
                CreditCardId = card.Id,
                CommerceId = null
            });

            var cardReference = LastFour(card.CardNumber);
            await _transactionRecorder.RecordWithoutSaveAsync(new TransactionEntry
            {
                Amount = dto.Amount,
                Type = TransactionType.Credit,
                Origin = cardReference,
                Beneficiary = account.AccountNumber,
                SourceAccountNumber = cardReference,
                DestinationAccountNumber = account.AccountNumber,
                Description = "Avance de efectivo",
                SavingAccountId = account.Id,
                Status = TransactionStatus.Approved,
                PerformedByUserId = account.UserId
            });

            await _unitOfWork.SaveChangesAsync();
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

    #region Shared financial helpers

        private async Task<SavingsAccount> GetOwnedActiveAccountAsync(string clientId, string accountNumber)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(accountNumber)
                ?? throw new InactiveAccountException();

            if (account.UserId != clientId || account.Status != AccountStatus.Active)
                throw new InactiveAccountException();

            var client = await _userService.GetByIdAsync(clientId);
            if (client == null || !client.IsActive)
                throw new InvalidOperationException("El usuario se encuentra inactivo y no puede realizar transacciones.");

            return account;
        }

        private async Task ReserveIdempotencyAsync(string operation, string actorUserId, string? key)
        {
            if (_idempotencyRepository is null || string.IsNullOrWhiteSpace(key))
                return;

            var normalizedKey = key.Trim();
            var normalizedActor = string.IsNullOrWhiteSpace(actorUserId) ? "anonymous" : actorUserId;
            if (await _idempotencyRepository.GetAsync(operation, normalizedKey, normalizedActor) is not null)
                throw new DuplicateOperationException();

            await _idempotencyRepository.AddWithoutSaveAsync(new IdempotencyRecord
            {
                Operation = operation,
                Key = normalizedKey,
                ActorUserId = normalizedActor,
                CreatedAt = _dateTimeProvider.UtcNow
            });

            // Claim the unique key before changing balances. A concurrent duplicate fails here,
            // while the surrounding financial transaction remains untouched.
            await _unitOfWork.SaveChangesAsync();
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
            Status = TransactionStatus.Approved,
            PerformedByUserId = source.UserId
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
            Status = TransactionStatus.Approved,
            PerformedByUserId = source.UserId
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
                Status = TransactionStatus.Declined,
                PerformedByUserId = source.UserId
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
