using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Core.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ABP.Core.Application.Interfaces.Services
{
    public class PaymentProcessorService : IPaymentProcessorService
    {
        private readonly ICreditCardService _creditCardService;
        private readonly ICommerceService _commerceService;
        private readonly ICreditCardConsumptionService _consumptionService;
        private readonly ISavingsAccountService _accountService;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserReadOnlyService _userService;
        private readonly IEmailServices _emailService;
        private readonly IIdempotencyRepository? _idempotencyRepository;
        private readonly ILogger<PaymentProcessorService> _logger;

        public PaymentProcessorService(
            ICreditCardService creditCardService,
            ICommerceService commerceService,
            ICreditCardConsumptionService consumptionService,
            ISavingsAccountService accountService,
            ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            IUserReadOnlyService userService,
            IEmailServices emailService,
            IIdempotencyRepository? idempotencyRepository = null,
            ILogger<PaymentProcessorService>? logger = null)
        {
            _creditCardService = creditCardService;
            _commerceService = commerceService;
            _consumptionService = consumptionService;
            _accountService = accountService;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _userService = userService;
            _emailService = emailService;
            _idempotencyRepository = idempotencyRepository;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentProcessorService>.Instance;
        }

        public async Task<PaymentResultDto> ProcessPaymentAsync(int commerceId, ProcessPaymentDto paymentDto)
        {
            if (paymentDto == null || paymentDto.TransactionAmount <= 0)
                return Failure("Transaction amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(paymentDto.CardNumber) ||
                paymentDto.CardNumber.Length != 16 ||
                !paymentDto.CardNumber.All(char.IsDigit))
            {
                return Failure("Invalid card number.");
            }

            var commerce = await _commerceService.GetByIdAsync(commerceId);
            if (commerce == null || !commerce.IsActive)
                return Failure("Commerce not found or inactive.");

            var card = await _creditCardService.GetByCardNumberAsync(paymentDto.CardNumber);
            if (card == null)
                return Failure("Invalid card number.");

            if (card.Status != CardStatus.Active)
            {
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("Card is not active.");
            }

            var cardOwner = await _userService.GetByIdAsync(card.ClientId);
            if (cardOwner == null || !cardOwner.IsActive)
            {
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("Card owner is inactive.");
            }

            if (!IsMatchingAndCurrentExpiration(card.ExpirationDate, paymentDto))
            {
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("Card expiration is invalid or has expired.");
            }

            if (!await _creditCardService.VerifyCvcAsync(card.Id, paymentDto.CVC))
            {
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("Invalid card security code.");
            }

            var commerceUserId = await _commerceService.GetActiveUserIdAsync(commerceId);
            if (string.IsNullOrWhiteSpace(commerceUserId))
            {
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("Commerce does not have an active user.");
            }

            var commerceAccount = await _accountService.GetPrimaryAccountByClientIdAsync(commerceUserId);
            if (commerceAccount == null || commerceAccount.Status != AccountStatus.Active)
            {
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("Commerce does not have an active settlement account.");
            }

            if (paymentDto.TransactionAmount > card.AvailableBalance)
            {
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("Insufficient credit limit.");
            }

            var consumption = new CreditCardConsumptionDto
            {
                Amount = paymentDto.TransactionAmount,
                TransactionDate = DateTime.UtcNow,
                CommerceName = commerce.Name,
                Status = ConsumptionStatus.Approved,
                CreditCardId = card.Id,
                CommerceId = commerceId
            };

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            if (!await ReserveIdempotencyWithoutSaveAsync("hermes.pay", commerceUserId, paymentDto.IdempotencyKey))
            {
                await transaction.RollbackAsync();
                return Failure("This payment request has already been processed.");
            }

            if (!await _creditCardService.ChargeWithoutSaveAsync(card.Id, paymentDto.TransactionAmount))
            {
                await transaction.RollbackAsync();
                await RecordDeclinedConsumptionAsync(card.Id, commerceId, commerce.Name, paymentDto.TransactionAmount);
                return Failure("The payment could not be authorized.");
            }

            // Keep the consumption in the same DbContext transaction. AddAsync would
            // flush it immediately and split Hermes Pay into multiple database writes.
            var savedConsumption = await _consumptionService.AddWithoutSaveAsync(consumption);

            commerceAccount.Balance += paymentDto.TransactionAmount;
            await _accountService.UpdateWithoutSaveAsync(commerceAccount);

            await _transactionRepository.AddWithoutSaveAsync(new Transaction
            {
                Amount = paymentDto.TransactionAmount,
                TransactionDate = DateTime.UtcNow,
                Type = TransactionType.Credit,
                Origin = "HERMES",
                Beneficiary = commerce.Name,
                SourceAccountNumber = "HERMES",
                DestinationAccountNumber = commerceAccount.AccountNumber,
                Description = $"Hermes Pay settlement - {commerce.Name}",
                Status = TransactionStatus.Approved,
                SavingAccountId = commerceAccount.Id,
                PerformedByUserId = commerceUserId,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            if (savedConsumption.Id == 0)
            {
                savedConsumption = (await _consumptionService.GetByCardIdAsync(card.Id))
                    .Where(c => c.CommerceId == commerceId &&
                                c.Status == ConsumptionStatus.Approved &&
                                c.Amount == paymentDto.TransactionAmount)
                    .OrderByDescending(c => c.TransactionDate)
                    .FirstOrDefault() ?? savedConsumption;
            }

            await SendNotificationsAsync(card.ClientId, commerceUserId, commerce.Name, paymentDto.TransactionAmount);

            return new PaymentResultDto
            {
                Success = true,
                Message = "Payment processed successfully.",
                TransactionId = savedConsumption.Id,
                NewBalance = card.AvailableBalance - paymentDto.TransactionAmount
            };
        }

        public async Task<PaginatedResult<PaymentTransactionDto>> GetCommerceTransactionsAsync(int commerceId, int page, int pageSize)
        {
            var commerce = await _commerceService.GetByIdAsync(commerceId) 
                ?? throw new KeyNotFoundException("El comercio no existe.");

            if (!commerce.IsActive)
                throw new InactiveCommerceException();

            var consumptions = await _consumptionService.GetByCommerceIdAsync(commerceId);
            var query = consumptions.AsQueryable();

            int totalCount = query.Count();
            var pagedConsumptions = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var items = new List<PaymentTransactionDto>();
            foreach (var c in pagedConsumptions)
            {
                string last4 = "****";
                try 
                {
                    var card = await _creditCardService.GetByIdAsync(c.CreditCardId);
                    if (card != null && card.CardNumber.Length >= 4)
                        last4 = card.CardNumber[^4..];
                }
                catch { /* Ignore if card not found */ }

                items.Add(new PaymentTransactionDto
                {
                    Id = c.Id,
                    Amount = c.Amount,
                    TransactionDate = c.TransactionDate,
                    CardNumber = last4,
                    Description = c.CommerceName,
                    Status = c.Status == ConsumptionStatus.Approved ? TransactionStatus.Approved : TransactionStatus.Declined
                });
            }

            return new PaginatedResult<PaymentTransactionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static PaymentResultDto Failure(string message) => new()
        {
            Success = false,
            Message = message
        };

        private async Task<bool> ReserveIdempotencyWithoutSaveAsync(string operation, string actorUserId, string? key)
        {
            if (_idempotencyRepository is null || string.IsNullOrWhiteSpace(key))
                return true;

            var normalizedKey = key.Trim();
            var normalizedActor = string.IsNullOrWhiteSpace(actorUserId) ? "anonymous" : actorUserId;
            if (await _idempotencyRepository.GetAsync(operation, normalizedKey, normalizedActor) is not null)
                return false;

            await _idempotencyRepository.AddWithoutSaveAsync(new IdempotencyRecord
            {
                Operation = operation,
                Key = normalizedKey,
                ActorUserId = normalizedActor,
                CreatedAt = DateTime.UtcNow
            });

            return true;
        }

        private async Task RecordDeclinedConsumptionAsync(int cardId, int commerceId, string commerceName, decimal amount)
        {
            try
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();
                await _consumptionService.AddWithoutSaveAsync(new CreditCardConsumptionDto
                {
                    Amount = amount,
                    TransactionDate = DateTime.UtcNow,
                    CommerceName = commerceName,
                    Status = ConsumptionStatus.Rejected,
                    CreditCardId = cardId,
                    CommerceId = commerceId
                });
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to record a declined Hermes Pay consumption for card {CardId} and commerce {CommerceId}.", cardId, commerceId);
            }
        }

        private static bool IsMatchingAndCurrentExpiration(string storedExpiration, ProcessPaymentDto payment)
        {
            if (!DateTime.TryParseExact(
                    storedExpiration,
                    "MM/yy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var expiration))
            {
                return false;
            }

            if (!int.TryParse(payment.YearExpirationCard, NumberStyles.None, CultureInfo.InvariantCulture, out var inputYear) ||
                !int.TryParse(payment.MonthExpirationCard, NumberStyles.None, CultureInfo.InvariantCulture, out var inputMonth))
            {
                return false;
            }

            var expirationMonth = expiration.Month;
            var expirationYear = expiration.Year;
            var lastValidMoment = new DateTime(expirationYear, expirationMonth, DateTime.DaysInMonth(expirationYear, expirationMonth), 23, 59, 59, DateTimeKind.Utc);

            return inputMonth == expirationMonth &&
                   inputYear == expirationYear &&
                   DateTime.UtcNow <= lastValidMoment;
        }

        private async Task SendNotificationsAsync(string cardOwnerId, string commerceUserId, string commerceName, decimal amount)
        {
            var recipients = new[]
            {
                (UserId: cardOwnerId, Subject: "Hermes Pay: pago aprobado", Body: $"Tu pago de {amount:C2} en {commerceName} fue aprobado el {DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")}."),
                (UserId: commerceUserId, Subject: "Hermes Pay: pago recibido", Body: $"Recibiste un pago de {amount:C2} en {commerceName} el {DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")}.")
            };

            foreach (var recipient in recipients)
            {
                try
                {
                    var user = await _userService.GetByIdAsync(recipient.UserId);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        await _emailService.SendAsync(user.Email, recipient.Subject, recipient.Body);
                }
                catch (Exception ex)
                {
                    // El pago ya fue confirmado; una falla de correo no debe revertirlo.
                    _logger.LogWarning(ex, "Hermes Pay notification failed for user {UserId}.", recipient.UserId);
                }
            }
        }
    }
}
