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
        ILogger<TransactionService> logger,
        IIdempotencyRepository? idempotencyRepository = null) : ITransactionService
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
        private readonly IIdempotencyRepository? _idempotencyRepository = idempotencyRepository;
        private static readonly CultureInfo _currencyCulture = CultureInfo.GetCultureInfo("es-DO");
        #endregion

        public async Task<TransactionDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity is null)
                return new TransactionDto();

            return MapWithFallback(entity);
        }

        public async Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int savingsAccountId)
        {
            var entities = await _repo.GetByAccountIdAsync(savingsAccountId);
            return MapWithFallback(entities);
        }

        public async Task<IEnumerable<TransactionDto>> GetByAccountIdsAsync(IEnumerable<int> savingsAccountIds)
        {
            var entities = await _repo.GetByAccountIdsAsync(savingsAccountIds);
            return MapWithFallback(entities);
        }

        public async Task<IEnumerable<TransactionDto>> GetHistoryAsync(int take = 100)
        {
            var entities = await _repo.GetRecentAsync(take);
            return MapWithFallback(entities);
        }

        private TransactionDto MapWithFallback(Transaction entity)
        {
            var dto = _mapper.Map<TransactionDto>(entity);
            if (dto.TransactionDate == default)
                dto.TransactionDate = dto.CreatedAt;

            return dto;
        }

        private IEnumerable<TransactionDto> MapWithFallback(IEnumerable<Transaction> entities)
        {
            return entities.Select(MapWithFallback).ToList();
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
                await RecordRejectedAsync(source, destination.AccountNumber, "Transaccion Express rechazada", dto.Amount);
                throw new AmountExceedsBalanceException();
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("client.express", dto.ClientId, dto.IdempotencyKey);

            source.Balance -= dto.Amount;
            destination.Balance += dto.Amount;
            await _accountRepo.UpdateAsync(source);
            await _accountRepo.UpdateAsync(destination);

            await _transactionRecorder.RecordDoubleEntryAsync(
                BuildTransferDebit(source, destination, "Transaccion Express", dto.Amount),
                BuildTransferCredit(destination, source, "Transaccion Express", dto.Amount));

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
            await ReserveIdempotencyAsync("client.card-payment", dto.ClientId, dto.IdempotencyKey);

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
                Description = "Pago a tarjeta de credito",
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
                Description = "Pago a prestamo",
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
            await _accountRepo.UpdateAsync(source);
            await _accountRepo.UpdateAsync(destination);

            await _transactionRecorder.RecordDoubleEntryAsync(
                BuildTransferDebit(source, destination, "Transaccion a beneficiario", dto.Amount),
                BuildTransferCredit(destination, source, "Transaccion a beneficiario", dto.Amount));

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
            await ReserveIdempotencyAsync("client.cash-advance", dto.ClientId, dto.IdempotencyKey);

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
                throw new InvalidOperationException("La cuenta de destino no existe.");

            if (account.Status != AccountStatus.Active)
                throw new InvalidOperationException("No se puede depositar en una cuenta inactiva o cancelada.");
            await using var depositTx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("cashier.deposit", cashierDepositDto.PerformedByUserId, cashierDepositDto.IdempotencyKey);
            account.Balance += cashierDepositDto.Amount;
            await _accountRepo.UpdateAsync(account);

            var transaction = new Transaction
            {
                Amount = cashierDepositDto.Amount,
                Type = TransactionType.Credit,
                TransactionDate = DateTime.UtcNow,
                Origin = "CAJERO",
                Beneficiary = cashierDepositDto.AccountNumber,
                Status = TransactionStatus.Approved,
                DestinationAccountNumber = cashierDepositDto.AccountNumber,
                SourceAccountNumber = "CASHIER",
                Description = "Deposito en caja",
                CreatedAt = DateTime.UtcNow,
                SavingAccountId = account.Id,
                PerformedByUserId = cashierDepositDto.PerformedByUserId
            };
            await _repo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            await depositTx.CommitAsync();
            var user = await _userService.GetByIdAsync(account.UserId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Deposito Recibido",
                        $"Un deposito de {cashierDepositDto.Amount:C2} se ha credito a tu cuenta {cashierDepositDto.AccountNumber}.");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Email notification failed."); }
            }
        }

        public async Task WithdrawAsync(CashierWithdrawalDto dto)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(dto.AccountNumber);

            if (account == null)
                throw new Exception("La cuenta de origen no existe.");

            if (account.Status != AccountStatus.Active)
                throw new InvalidOperationException("No se puede retirar dinero de una cuenta inactiva o cancelada.");
            if (account.Balance < dto.Amount)
            {
                throw new InvalidOperationException($"Fondos insuficientes. Balance actual: ${account.Balance:N2}");
            }

            await using var withdrawalTx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("cashier.withdrawal", dto.PerformedByUserId, dto.IdempotencyKey);
            account.Balance -= dto.Amount;
            await _accountRepo.UpdateWithoutSaveAsync(account);
            var transaction = new Transaction
            {
                Amount = dto.Amount,
                Type = TransactionType.Debit,
                TransactionDate = DateTime.UtcNow,
                Origin = dto.AccountNumber,
                Beneficiary = "CAJERO",
                Status = TransactionStatus.Approved,
                SourceAccountNumber = dto.AccountNumber,
                DestinationAccountNumber = "CAJERO",
                Description = "Retiro de efectivo realizado en sucursal",
                CreatedAt = DateTime.UtcNow,
                SavingAccountId = account.Id,
                PerformedByUserId = dto.PerformedByUserId
            };
            await _repo.AddWithoutSaveAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            await withdrawalTx.CommitAsync();

            var user = await _userService.GetByIdAsync(account.UserId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Notificación de Retiro",
                        $"Se ha procesado un retiro de {dto.Amount:C2} de su cuenta {dto.AccountNumber}.");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Email notification failed."); }
            }
        }


        public async Task CashierPayCreditCardAsync(CashierPayCreditCardDto dto)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber);
            if (account == null)
                throw new Exception("La cuenta de origen no existe.");

            if (account.Status != AccountStatus.Active)
                throw new InvalidOperationException("No se puede procesar el pago desde una cuenta inactiva o cancelada.");

            var card = await _creditCardRepo.GetByCardNumberAsync(dto.CardNumber);
            if (card == null)
                throw new Exception("Tarjeta de credito no encontrada.");

            if (card.Status != CardStatus.Active)
                throw new InvalidOperationException("No se pueden procesar pagos para una tarjeta inactiva o cancelada.");

            if (account.Balance < dto.Amount)
                throw new InvalidOperationException("Fondos insuficientes en la cuenta de origen.");

            if (card.AmountOwed <= 0)
                throw new InvalidOperationException("Esta tarjeta no tiene deuda pendiente.");

            var actualPayment = Math.Min(dto.Amount, card.AmountOwed);

            await using var cardPaymentTx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("cashier.card-payment", dto.PerformedByUserId, dto.IdempotencyKey);
            account.Balance -= actualPayment;
            card.AmountOwed -= actualPayment;

            await _accountRepo.UpdateWithoutSaveAsync(account);
            await _creditCardRepo.UpdateWithoutSaveAsync(card);

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
                Description = "Pago de tarjeta de credito realizado en sucursal",
                CreatedAt = DateTime.UtcNow,
                PerformedByUserId = dto.PerformedByUserId
            };
            await _repo.AddWithoutSaveAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            await cardPaymentTx.CommitAsync();

            var user = await _userService.GetByIdAsync(card.ClientId);
            if (user != null)
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Pago de tarjeta de credito recibido",
                        $"Se ha aplicado un pago de {actualPayment:C2} a su tarjeta terminada en {card.CardNumber.Substring(card.CardNumber.Length - 4)}.");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Email notification failed."); }
            }
        }


        public async Task CashierPayLoanAsync(CashierPayLoanDto Dto)
        {
            var account = await _accountRepo.GetByAccountNumberAsync(Dto.SourceAccountNumber);
            var loan = await _loanRepo.GetByLoanNumberAsync(Dto.LoanNumber);

            if (account == null || loan == null) throw new Exception("Cuenta o Prestamo no encontrado.");
            if (account.Balance < Dto.Amount) throw new InvalidOperationException("Fondos insuficientes en la cuenta de origen.");

            var installments = (await _installmentRepo.GetByLoanIdAsync(loan.Id))
                .Where(i => i.Status != InstallmentStatus.Paid).OrderBy(i => i.DueDate).ToList();

            await using var loanPaymentTx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("cashier.loan-payment", Dto.PerformedByUserId, Dto.IdempotencyKey);

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

                await _installmentRepo.UpdateWithoutSaveAsync(installment);
            }
            account.Balance -= totalActuallyPaid;
            await _accountRepo.UpdateWithoutSaveAsync(account);

            var stillPending = installments.Any(i => i.Status != InstallmentStatus.Paid);

            if (!stillPending)
            {
                loan.Status = LoanStatus.Completed;
                await _loanRepo.UpdateWithoutSaveAsync(loan);
            }
            await _repo.AddWithoutSaveAsync(new Transaction
            {
                Amount = totalActuallyPaid,
                Type = TransactionType.Debit,
                SourceAccountNumber = Dto.SourceAccountNumber,
                DestinationAccountNumber = Dto.LoanNumber,
                Description = $"Pago de prestamo aplicado a {loan.LoanNumber}",
                CreatedAt = DateTime.UtcNow,
                SavingAccountId = account.Id,
                Status = TransactionStatus.Approved,
                TransactionDate = DateTime.UtcNow,
                Origin = Dto.SourceAccountNumber,
                Beneficiary = Dto.LoanNumber,
                PerformedByUserId = Dto.PerformedByUserId
            });
            await _unitOfWork.SaveChangesAsync();
            await loanPaymentTx.CommitAsync();
            var user = await _userService.GetByIdAsync(loan.ClientId);
            await _emailService.SendAsync(user.Email, "Pago de prestamo aplicado",
                $"Se ha aplicado un pago de {totalActuallyPaid:C2} a su prestamo {loan.LoanNumber}.");
        }


        public async Task CashierTransferAsync(CashierTransferDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("El monto de la transferencia debe ser mayor que cero.");

            if (dto.SourceAccountNumber == dto.DestinationAccountNumber)
                throw new InvalidOperationException("Las cuentas de origen y destino no pueden ser iguales.");

            var sourceAccount = await _accountRepo.GetByAccountNumberAsync(dto.SourceAccountNumber)
                ?? throw new Exception("Cuenta de origen no encontrada.");

            var destAccount = await _accountRepo.GetByAccountNumberAsync(dto.DestinationAccountNumber)
                ?? throw new Exception("Cuenta de destino no encontrada.");

            if (sourceAccount.Status != AccountStatus.Active || destAccount.Status != AccountStatus.Active)
                throw new InvalidOperationException("Ambas cuentas deben estar activas.");

            if (sourceAccount.Balance < dto.Amount)
                throw new InvalidOperationException("Fondos insuficientes en la cuenta de origen.");

            await using var cashierTransferTx = await _unitOfWork.BeginTransactionAsync();
            await ReserveIdempotencyAsync("cashier.transfer", dto.PerformedByUserId, dto.IdempotencyKey);
            sourceAccount.Balance -= dto.Amount;
            destAccount.Balance += dto.Amount;

            await _accountRepo.UpdateWithoutSaveAsync(sourceAccount);
            await _accountRepo.UpdateWithoutSaveAsync(destAccount);

            await _repo.AddWithoutSaveAsync(new Transaction
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
                Description = $"Transferencia enviada a {dto.DestinationAccountNumber}",
                CreatedAt = DateTime.UtcNow,
                PerformedByUserId = dto.PerformedByUserId
            });

            await _repo.AddWithoutSaveAsync(new Transaction
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
                Description = $"Transferencia recibida desde {dto.SourceAccountNumber}",
                CreatedAt = DateTime.UtcNow,
                PerformedByUserId = dto.PerformedByUserId
            });

            await _unitOfWork.SaveChangesAsync();
            await cashierTransferTx.CommitAsync();

            var sourceUser = await _userService.GetByIdAsync(sourceAccount.UserId);
            var destUser = await _userService.GetByIdAsync(destAccount.UserId);

            if (sourceUser != null)
            {
                try
                {
                    await _emailService.SendAsync(sourceUser.Email, "Transferencia Enviada",
                        $"Ha enviado {dto.Amount:C2} a la cuenta {dto.DestinationAccountNumber}.");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Email notification failed."); }
            }

            if (destUser != null)
            {
                try
                {
                    await _emailService.SendAsync(destUser.Email, "Transferencia Recibida",
                        $"Ha recibido {dto.Amount:C2} desde la cuenta {dto.SourceAccountNumber}.");
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Email notification failed."); }
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
