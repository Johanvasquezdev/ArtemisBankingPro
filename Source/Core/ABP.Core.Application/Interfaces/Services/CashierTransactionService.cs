using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Exceptions;
using ABP.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ABP.Core.Application.Interfaces.Services;

internal sealed class CashierTransactionService : ICashierTransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly ISavingsAccountRepository _accountRepo;
    private readonly IUserReadOnlyService _userService;
    private readonly IEmailServices _emailService;
    private readonly ICreditCardRepository _creditCardRepo;
    private readonly ILoanRepository _loanRepo;
    private readonly ILoanInstallmentRepository _installmentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly IIdempotencyRepository? _idempotencyRepository;

    public CashierTransactionService(TransactionOperationDependencies dependencies)
    {
        _repo = dependencies.TransactionRepository;
        _accountRepo = dependencies.SavingsAccountRepository;
        _userService = dependencies.UserService;
        _emailService = dependencies.EmailService;
        _creditCardRepo = dependencies.CreditCardRepository;
        _loanRepo = dependencies.LoanRepository;
        _installmentRepo = dependencies.LoanInstallmentRepository;
        _unitOfWork = dependencies.UnitOfWork;
        _dateTimeProvider = dependencies.DateTimeProvider;
        _logger = dependencies.Logger;
        _idempotencyRepository = dependencies.IdempotencyRepository;
    }

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
            await _accountRepo.UpdateWithoutSaveAsync(account);

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
            await _repo.AddWithoutSaveAsync(transaction);
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
                var declinedTx = new Transaction
                {
                    Amount = dto.Amount,
                    Type = TransactionType.Debit,
                    TransactionDate = DateTime.UtcNow,
                    Origin = dto.AccountNumber,
                    Beneficiary = "CAJERO",
                    Status = TransactionStatus.Declined,
                    SourceAccountNumber = dto.AccountNumber,
                    DestinationAccountNumber = "CAJERO",
                    Description = "Retiro rechazado por fondos insuficientes",
                    CreatedAt = DateTime.UtcNow,
                    SavingAccountId = account.Id,
                    PerformedByUserId = dto.PerformedByUserId
                };
                await _repo.AddWithoutSaveAsync(declinedTx);
                await _unitOfWork.SaveChangesAsync();
                throw new InvalidOperationException($"Fondos insuficientes. Balance actual: ");
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
            {
                var declinedTx = new Transaction
                {
                    Amount = dto.Amount,
                    TransactionDate = DateTime.UtcNow,
                    Type = TransactionType.Debit,
                    Origin = dto.SourceAccountNumber,
                    Beneficiary = dto.CardNumber,
                    Status = TransactionStatus.Declined,
                    SavingAccountId = account.Id,
                    SourceAccountNumber = dto.SourceAccountNumber,
                    DestinationAccountNumber = $"CARD-{card.CardNumber[^4..]}",
                    Description = "Pago de tarjeta rechazado por fondos insuficientes",
                    CreatedAt = DateTime.UtcNow,
                    PerformedByUserId = dto.PerformedByUserId
                };
                await _repo.AddWithoutSaveAsync(declinedTx);
                await _unitOfWork.SaveChangesAsync();
                throw new InvalidOperationException("Fondos insuficientes en la cuenta de origen.");
            }

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
            if (account.Balance < Dto.Amount)
            {
                var declinedTx = new Transaction
                {
                    Amount = Dto.Amount,
                    Type = TransactionType.Debit,
                    TransactionDate = DateTime.UtcNow,
                    Origin = Dto.SourceAccountNumber,
                    Beneficiary = Dto.LoanNumber,
                    Status = TransactionStatus.Declined,
                    SavingAccountId = account.Id,
                    SourceAccountNumber = Dto.SourceAccountNumber,
                    DestinationAccountNumber = Dto.LoanNumber,
                    Description = "Pago de prestamo rechazado por fondos insuficientes",
                    CreatedAt = DateTime.UtcNow,
                    PerformedByUserId = Dto.PerformedByUserId
                };
                await _repo.AddWithoutSaveAsync(declinedTx);
                await _unitOfWork.SaveChangesAsync();
                throw new InvalidOperationException("Fondos insuficientes en la cuenta de origen.");
            }

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
            if (user is not null)
            {
                await SendEmailSafeAsync(user.Email, "Pago de prestamo aplicado",
                    $"Se ha aplicado un pago de {totalActuallyPaid:C2} a su prestamo {loan.LoanNumber}.");
            }
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
            {
                var declinedTx = new Transaction
                {
                    Amount = dto.Amount,
                    Type = TransactionType.Debit,
                    TransactionDate = DateTime.UtcNow,
                    Origin = dto.SourceAccountNumber,
                    Beneficiary = dto.DestinationAccountNumber,
                    Status = TransactionStatus.Declined,
                    SavingAccountId = sourceAccount.Id,
                    SourceAccountNumber = dto.SourceAccountNumber,
                    DestinationAccountNumber = dto.DestinationAccountNumber,
                    Description = "Transferencia rechazada por fondos insuficientes",
                    CreatedAt = DateTime.UtcNow,
                    PerformedByUserId = dto.PerformedByUserId
                };
                await _repo.AddWithoutSaveAsync(declinedTx);
                await _unitOfWork.SaveChangesAsync();
                throw new InvalidOperationException("Fondos insuficientes en la cuenta de origen.");
            }

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

    #region Shared cashier helpers

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

    #endregion
}
