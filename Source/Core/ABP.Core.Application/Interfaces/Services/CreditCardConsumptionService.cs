using AutoMapper;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;

namespace ABP.Core.Application.Interfaces.Services
{
    public class CreditCardConsumptionService : ICreditCardConsumptionService
    {
        private readonly ICreditCardConsumptionRepository _repo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreditCardConsumptionService(ICreditCardConsumptionRepository repo, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreditCardConsumptionDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<CreditCardConsumptionDto>(entity);
        }

        public async Task<IEnumerable<CreditCardConsumptionDto>> GetByCardIdAsync(int creditCardId)
        {
            var entities = await _repo.GetByCardIdAsync(creditCardId);
            return _mapper.Map<IEnumerable<CreditCardConsumptionDto>>(entities);
        }

        public async Task<IEnumerable<CreditCardConsumptionDto>> GetByCommerceIdAsync(int commerceId)
        {
            var entities = await _repo.GetByCommerceIdAsync(commerceId);
            return _mapper.Map<IEnumerable<CreditCardConsumptionDto>>(entities);
        }

        public async Task<CreditCardConsumptionDto> AddAsync(CreditCardConsumptionDto dto)
        {
            var entity = _mapper.Map<CreditCardConsumption>(dto);
            entity.TransactionDate = DateTime.UtcNow;
            await _repo.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CreditCardConsumptionDto>(entity);
        }

        public async Task<CreditCardConsumptionDto> AddWithoutSaveAsync(CreditCardConsumptionDto dto)
        {
            var entity = _mapper.Map<CreditCardConsumption>(dto);
            entity.TransactionDate = dto.TransactionDate == default ? DateTime.UtcNow : dto.TransactionDate;
            await _repo.AddWithoutSaveAsync(entity);
            return _mapper.Map<CreditCardConsumptionDto>(entity);
        }
    }
}
