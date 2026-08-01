using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using System.Security.Cryptography;
using System.Text;

namespace ABP.Core.Application.Interfaces.Services
{
    public class CreditCardService(ICreditCardRepository repo, ICreditCardConsumptionRepository consumptionService,ISavingsAccountRepository accountRepo, IMapper mapper, IUserReadOnlyService user, IEmailServices email) : ICreditCardService
    {
        private readonly ICreditCardRepository _repo = repo;
        private readonly ICreditCardConsumptionRepository _consumptionRepo = consumptionService;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly IMapper _mapper = mapper;
        private readonly IUserReadOnlyService _userService = user;
        private readonly IEmailServices _emailService = email;

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

        public async Task<IEnumerable<CreditCardDto>> GetActiveByClientIdAsync(string clientId)
        {
            var entities = await _repo.GetActiveCardsByClientIdAsync(clientId);
            return _mapper.Map<IEnumerable<CreditCardDto>>(entities);
        }

        public async Task<PaginatedResult<CreditCardDto>> GetAllPagedAsync(int page, int pageSize = 20, CardStatus? status = null, string? cedula = null)
        {
            var entities = await _repo.GetAllPagedAsync(page, pageSize, status, cedula);
            var items = _mapper.Map<IEnumerable<CreditCardDto>>(entities);

            foreach (var item in items)
            {
                var user = await _userService.GetByIdAsync(item.ClientId);
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
                AssignedByAdminId = string.Empty
            };

            await _repo.AddAsync(card);
            return _mapper.Map<CreditCardDto>(card);
        }

        public async Task<bool> ChangeStatusAsync(int cardId, CardStatus status)
        {
            var entity = await _repo.GetByIdAsync(cardId);
            if (entity == null) return false;

            entity.Status = status;
            await _repo.UpdateAsync(entity);
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

            await _accountRepo.UpdateAsync(account);
            await _repo.UpdateAsync(card);
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

            await _repo.UpdateAsync(card);
            await _accountRepo.UpdateAsync(account);

            await _consumptionRepo.AddAsync(new CreditCardConsumption
            {
                Amount = dto.Amount,
                TransactionDate = DateTime.UtcNow,
                CommerceName = "AVANCE",
                Status = ConsumptionStatus.Approved,
                CreditCardId = dto.CreditCardId,
                CommerceId = null
            });

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
            if (card == null) throw new Exception("Credit card not found.");

            if (newCreditLimit < card.AmountOwed)
            {
                throw new InvalidOperationException($"The new limit cannot be lower than the current debt (${card.AmountOwed:N2}).");
            }

            card.CreditLimit = newCreditLimit;
            await _repo.UpdateAsync(card);

            var user = await _userService.GetByIdAsync(card.ClientId);
            await _emailService.SendAsync(user.Email, "Credit Limit Updated", $"Your new limit is {newCreditLimit:C2}");
        }

        public async Task CancelAsync(int cardId)
        {
            var card = await _repo.GetByIdAsync(cardId);
            if (card == null) throw new Exception("Credit card not found.");

            if (card.Status == CardStatus.Cancelled) return;

            if (card.AmountOwed > 0)
            {
                throw new InvalidOperationException($"Cannot cancel card. Client owes ${card.AmountOwed:N2}. The balance must be zero.");
            }

            card.Status = CardStatus.Cancelled;
            card.CreditLimit = 0;

            await _repo.UpdateAsync(card);

            var user = await _userService.GetByIdAsync(card.ClientId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    await _emailService.SendAsync(user.Email, "Credit Card Cancelled", "Your credit card has been successfully closed.");
                }
                catch
                {
                    // Logging in error, email not working, but cancellation not reversed
                }
            }
        }
    }
}
