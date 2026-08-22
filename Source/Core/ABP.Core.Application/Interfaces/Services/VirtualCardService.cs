using ABP.Core.Application.DTOs.VirtualCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using System.Security.Cryptography;

namespace ABP.Core.Application.Interfaces.Services
{
    public class VirtualCardService(IVirtualCardRepository repo, IMapper mapper, IUnitOfWork unitOfWork) : IVirtualCardService
    {
        private readonly IVirtualCardRepository _repo = repo;
        private readonly IMapper _mapper = mapper;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<VirtualCardDto> CreateAsync(CreateVirtualCardDto dto)
        {
            var virtualCard = new VirtualCard
            {
                SavingsAccountId = dto.SavingsAccountId,
                LimitAmount = dto.LimitAmount,
                CardNumber = GenerateVisaCardNumber(),
                CVV = RandomNumberGenerator.GetInt32(100, 1000).ToString(),
                ExpirationDate = DateTime.UtcNow.AddYears(5),
                IsActive = true,
                IsFrozen = false
            };

            await _repo.AddAsync(virtualCard);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<VirtualCardDto>(virtualCard);
        }

        private static string GenerateVisaCardNumber()
        {
            return $"4{RandomNumberGenerator.GetInt32(0, 1000):D3}" +
                   $"{RandomNumberGenerator.GetInt32(0, 10000):D4}" +
                   $"{RandomNumberGenerator.GetInt32(0, 10000):D4}" +
                   $"{RandomNumberGenerator.GetInt32(0, 10000):D4}";
        }

        public async Task<VirtualCardDto> GetByIdAsync(int id)
        {
            var card = await _repo.GetByIdAsync(id);
            return _mapper.Map<VirtualCardDto>(card);
        }

        public async Task<List<VirtualCardDto>> GetBySavingsAccountIdAsync(int accountId)
        {
            var cards = await _repo.GetAllAsync();
            var filtered = cards.Where(c => c.SavingsAccountId == accountId).ToList();
            return _mapper.Map<List<VirtualCardDto>>(filtered);
        }

        public async Task ToggleFreezeAsync(int id)
        {
            var card = await _repo.GetByIdAsync(id);
            if (card != null)
            {
                card.IsFrozen = !card.IsFrozen;
                await _repo.UpdateAsync(card);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
