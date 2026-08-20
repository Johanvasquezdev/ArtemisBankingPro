using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ABP.Core.Application.Interfaces.Services
{
    public class CreditCardService(ICreditCardRepository repo, ICreditCardConsumptionRepository consumptionService, ISavingsAccountRepository accountRepo, IMapper mapper, IUserReadOnlyService user, IEmailServices email, ILogger<CreditCardService> logger, IUnitOfWork unitOfWork) : ICreditCardService
    {
        private readonly ICreditCardRepository _repo = repo;
        private readonly ICreditCardConsumptionRepository _consumptionRepo = consumptionService;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly IMapper _mapper = mapper;
        private readonly IUserReadOnlyService _userService = user;
        private readonly IEmailServices _emailService = email;
        private readonly ILogger<CreditCardService> _logger = logger;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<CreditCardDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<CreditCardDto>(entity);
        }

        public async Task<CreditCardDto?> GetByCardNumberAsync(string cardNumber)
        {
            var entity = await _repo.GetByCardNumberAsync(cardNumber);
            return entity is null ? null : _mapper.Map<CreditCardDto>(entity);
        }

        public async Task<bool> VerifyCvcAsync(int cardId, string cvc)
        {
            if (string.IsNullOrWhiteSpace(cvc) || cvc.Length != 3 || !cvc.All(char.IsDigit))
                return false;

            var card = await _repo.GetByIdAsync(cardId);
            if (card == null || string.IsNullOrWhiteSpace(card.CVCHash))
                return false;

            var actualHash = Convert.FromHexString(card.CVCHash);
            var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(cvc));
            return CryptographicOperations.FixedTimeEquals(actualHash, providedHash);
        }

        public async Task<bool> ChargeAsync(int cardId, decimal amount)
        {
            var charged = await ChargeWithoutSaveAsync(cardId, amount);
            if (charged)
                await _unitOfWork.SaveChangesAsync();
            return charged;
        }

        public async Task<bool> ChargeWithoutSaveAsync(int cardId, decimal amount)
        {
            if (amount <= 0) return false;

            var card = await _repo.GetByIdAsync(cardId);
            if (card == null || card.Status != CardStatus.Active)
                return false;

            if (amount > card.CreditLimit - card.AmountOwed)
                return false;

            card.AmountOwed += amount;
            await _repo.UpdateWithoutSaveAsync(card);
            return true;
        }

        public async Task<IEnumerable<CreditCardDto>> GetActiveByClientIdAsync(string clientId)
        {
            var entities = await _repo.GetActiveCardsByClientIdAsync(clientId);
            return _mapper.Map<IEnumerable<CreditCardDto>>(entities);
        }

        public async Task<PaginatedResult<CreditCardDto>> GetAllPagedAsync(int page, int pageSize = 20, CardStatus? status = null, string? cedula = null)
        {
            var clientId = string.IsNullOrWhiteSpace(cedula)
                ? null
                : await _userService.GetUserIdByCedulaAsync(cedula);
            var entities = await _repo.GetAllPagedAsync(page, pageSize, status, clientId);
            var items = _mapper.Map<IEnumerable<CreditCardDto>>(entities);
            var usersById = (await _userService.GetByIdsAsync(items.Select(item => item.ClientId)))
                .ToDictionary(user => user.Id);

            foreach (var item in items)
            {
                usersById.TryGetValue(item.ClientId, out var user);
                if (user != null)
                    item.ClientFullName = $"{user.FirstName} {user.LastName}";
            }
            var totalCount = await _repo.GetTotalActiveCardsCountAsync();

            return new PaginatedResult<CreditCardDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CreditCardDto> AssignAsync(AssignCreditCardDto dto)
        {
            string cardNumber;
            do
            {
                cardNumber = GenerateCardNumber();
            }
            while (await _repo.CardNumberExistsAsync(cardNumber));

            var cvc = Random.Shared.Next(100, 999).ToString();
            var cvcHash = HashCvc(cvc);

            var card = new CreditCard
            {
                CardNumber = cardNumber,
                CreditLimit = dto.CreditLimit,
                ExpirationDate = DateTime.UtcNow.AddYears(5).ToString("MM/yy"),
                AmountOwed = 0,
                CVCHash = cvcHash,
                Status = CardStatus.Active,
                CreatedAt = DateTime.UtcNow,
                ClientId = dto.ClientId,
                AssignedByAdminId = dto.AdminId
            };

            await using var assignmentTransaction = await _unitOfWork.BeginTransactionAsync();
            await _repo.AddWithoutSaveAsync(card);
            await _unitOfWork.SaveChangesAsync();
            await assignmentTransaction.CommitAsync();
            
            var user = await _userService.GetByIdAsync(dto.ClientId);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    _logger.LogInformation("===============================================");
                    _logger.LogInformation("TESTING: CVC GENERADO PARA LA TARJETA {CardNumber}: {CVC}", cardNumber, cvc);
                    _logger.LogInformation("===============================================");

                    await _emailService.SendAsync(
                        user.Email,
                        "Nueva Tarjeta de Crédito Asignada",
                        $"Se le ha asignado una nueva tarjeta de crédito.<br>" +
                        $"Número: {cardNumber}<br>" +
                        $"Fecha de Expiración: {card.ExpirationDate}<br>" +
                        $"CVC: {cvc}<br><br>" +
                        $"Por favor guarde esta información de forma segura, ya que el CVC no podrá ser visualizado en el sistema por motivos de seguridad."
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Credit card assigned to user {UserId}, but the email notification failed.", dto.ClientId);
                }
            }

            return _mapper.Map<CreditCardDto>(card);
        }

        public async Task<bool> ChangeStatusAsync(int cardId, CardStatus status)
        {
            var entity = await _repo.GetByIdAsync(cardId);
            if (entity == null) return false;

            entity.Status = status;
            await _repo.UpdateWithoutSaveAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PayCreditCardAsync(string sourceAccountNumber, string cardNumber, decimal amount)
        {
            if (amount <= 0) return false;

            var account = await _accountRepo.GetByAccountNumberAsync(sourceAccountNumber);
            if (account == null || account.Status != AccountStatus.Active) return false;
            if (account.Balance < amount) return false;

            var card = await _repo.GetByCardNumberAsync(cardNumber);
            if (card == null || card.Status != CardStatus.Active) return false;

            var paymentAmount = Math.Min(amount, card.AmountOwed);
            account.Balance -= paymentAmount;
            card.AmountOwed -= paymentAmount;

            await using var paymentTransaction = await _unitOfWork.BeginTransactionAsync();
            await _accountRepo.UpdateWithoutSaveAsync(account);
            await _repo.UpdateWithoutSaveAsync(card);
            await _unitOfWork.SaveChangesAsync();
            await paymentTransaction.CommitAsync();
            return true;
        }

        public async Task<bool> CashAdvanceAsync(CashAdvanceDto dto)
        {
            if (dto.Amount <= 0) return false;

            var card = await _repo.GetByIdAsync(dto.CreditCardId);
            if (card == null || card.Status != CardStatus.Active) return false;

            var account = await _accountRepo.GetByIdAsync(dto.SavingsAccountId);
            if (account == null || account.Status != AccountStatus.Active) return false;

            var availableCredit = card.CreditLimit - card.AmountOwed;
            if (dto.Amount > availableCredit) return false;

            // 6.25% interest on cash advances
            var totalWithInterest = dto.Amount * 1.0625m;

            card.AmountOwed += totalWithInterest;
            account.Balance += dto.Amount;

            await using var advanceTransaction = await _unitOfWork.BeginTransactionAsync();
            await _repo.UpdateWithoutSaveAsync(card);
            await _accountRepo.UpdateWithoutSaveAsync(account);

            await _consumptionRepo.AddWithoutSaveAsync(new CreditCardConsumption
            {
                Amount = dto.Amount,
                TransactionDate = DateTime.UtcNow,
                CommerceName = "AVANCE",
                Status = ConsumptionStatus.Approved,
                CreditCardId = dto.CreditCardId,
                CommerceId = null
            });

            await _unitOfWork.SaveChangesAsync();
            await advanceTransaction.CommitAsync();
            return true;
        }

        public async Task<decimal> GetTotalDebtByClientIdAsync(string clientId)
        {
            return await _repo.GetTotalCardDebtByClientIdAsync(clientId);
        }

        public async Task<int> GetTotalActiveCardsCountAsync()
        {
            return await _repo.GetTotalActiveCardsCountAsync();
        }

        private static string GenerateCardNumber()
        {
            var rng = Random.Shared;
            return $"{rng.Next(1000, 9999)}{rng.Next(1000, 9999)}{rng.Next(1000, 9999)}{rng.Next(1000, 9999)}";
        }

        private static string HashCvc(string cvc)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(cvc));
            return Convert.ToHexStringLower(bytes);
        }

        public async Task UpdateLimitAsync(int cardId, decimal newCreditLimit)
        {
            var card = await _repo.GetByIdAsync(cardId);
            if (card == null) throw new InvalidOperationException("Tarjeta de credito no encontrada.");

            if (newCreditLimit <= 0)
                throw new InvalidOperationException("El limite de credito debe ser mayor que cero.");

            if (newCreditLimit < card.AmountOwed)
            {
                throw new InvalidOperationException($"El nuevo limite no puede ser menor que la deuda actual (${card.AmountOwed:N2}).");
            }

            card.CreditLimit = newCreditLimit;
            await _repo.UpdateWithoutSaveAsync(card);
            await _unitOfWork.SaveChangesAsync();

            var user = await _userService.GetByIdAsync(card.ClientId);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Credit limit updated for card {CardId}, but no valid client email was found.", cardId);
                return;
            }

            try
            {
                await _emailService.SendAsync(
                    user.Email,
                    "Limite de credito actualizado",
                    $"Su nuevo limite es {newCreditLimit:C2}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Credit limit updated for card {CardId}, but the email notification failed.", cardId);
            }
        }

        public async Task CancelAsync(int cardId)
        {
            var card = await _repo.GetByIdAsync(cardId) ?? throw new Exception("Tarjeta de credito no encontrada.");
            if (card.Status == CardStatus.Cancelled) return;

            if (card.AmountOwed > 0)
            {
                throw new InvalidOperationException($"No se puede cancelar la tarjeta. El cliente adeuda ${card.AmountOwed:N2}. El saldo debe ser cero.");
            }

            card.Status = CardStatus.Cancelled;
            card.CreditLimit = 0;

            await _repo.UpdateWithoutSaveAsync(card);
            await _unitOfWork.SaveChangesAsync();

            var user = await _userService.GetByIdAsync(card.ClientId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Tarjeta de credito cancelada", "Su tarjeta de credito ha sido cancelada exitosamente.");
                }
                catch
                {
                    // Registro de error, el correo no funciona, pero la cancelacion no se revierte
                }
            }
        }
    }
}
