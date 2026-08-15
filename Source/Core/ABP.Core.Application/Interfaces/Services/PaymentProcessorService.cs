using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
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

        public PaymentProcessorService(
            ICreditCardService creditCardService,
            ICommerceService commerceService,
            ICreditCardConsumptionService consumptionService,
            ISavingsAccountService accountService,
            ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            IUserReadOnlyService userService,
            IEmailServices emailService)
        {
            _creditCardService = creditCardService;
            _commerceService = commerceService;
            _consumptionService = consumptionService;
            _accountService = accountService;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _userService = userService;
            _emailService = emailService;
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
                return Failure("Card is not active.");

            if (!IsMatchingAndCurrentExpiration(card.ExpirationDate, paymentDto))
                return Failure("Card expiration is invalid or has expired.");

            if (!await _creditCardService.VerifyCvcAsync(card.Id, paymentDto.CVC))
                return Failure("Invalid card security code.");

            var commerceUserId = await _commerceService.GetActiveUserIdAsync(commerceId);
            if (string.IsNullOrWhiteSpace(commerceUserId))
                return Failure("Commerce does not have an active user.");

            var commerceAccount = await _accountService.GetPrimaryAccountByClientIdAsync(commerceUserId);
            if (commerceAccount == null || commerceAccount.Status != AccountStatus.Active)
                return Failure("Commerce does not have an active settlement account.");

            if (paymentDto.TransactionAmount > card.AvailableBalance)
                return Failure("Insufficient credit limit.");

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

            if (!await _creditCardService.ChargeAsync(card.Id, paymentDto.TransactionAmount))
            {
                await transaction.RollbackAsync();
                return Failure("The payment could not be authorized.");
            }

            var savedConsumption = await _consumptionService.AddAsync(consumption);

            commerceAccount.Balance += paymentDto.TransactionAmount;
            await _accountService.UpdateAsync(commerceAccount);

            await _transactionRepository.AddAsync(new Transaction
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
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            await SendNotificationsAsync(card.ClientId, commerceUserId, commerce.Name, paymentDto.TransactionAmount);

            return new PaymentResultDto
            {
                Success = true,
                Message = "Payment processed successfully.",
                TransactionId = savedConsumption.Id,
                NewBalance = card.AvailableBalance - paymentDto.TransactionAmount
            };
        }

        public async Task<IEnumerable<PaymentTransactionDto>> GetCommerceTransactionsAsync(int commerceId)
        {
            var commerce = await _commerceService.GetByIdAsync(commerceId);
            if (commerce == null)
                return Enumerable.Empty<PaymentTransactionDto>();

            var consumptions = await _consumptionService.GetByCommerceIdAsync(commerceId);

            return consumptions.Select(c => new PaymentTransactionDto
            {
                Id = c.Id,
                Amount = c.Amount,
                TransactionDate = c.TransactionDate,
                CardNumber = "****",
                Description = c.CommerceName,
                Status = c.Status == ConsumptionStatus.Approved ? TransactionStatus.Approved : TransactionStatus.Declined
            });
        }

        private static PaymentResultDto Failure(string message) => new()
        {
            Success = false,
            Message = message
        };

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
                (UserId: cardOwnerId, Subject: "Hermes Pay: pago aprobado", Body: $"Tu pago de {amount:C2} en {commerceName} fue aprobado."),
                (UserId: commerceUserId, Subject: "Hermes Pay: pago recibido", Body: $"Recibiste un pago de {amount:C2} en {commerceName}.")
            };

            foreach (var recipient in recipients)
            {
                try
                {
                    var user = await _userService.GetByIdAsync(recipient.UserId);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                        await _emailService.SendAsync(user.Email, recipient.Subject, recipient.Body);
                }
                catch
                {
                    // El pago ya fue confirmado; una falla de correo no debe revertirlo.
                }
            }
        }
    }
}
