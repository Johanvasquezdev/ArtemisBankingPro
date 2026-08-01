using AutoMapper;
using ABP.Core.Application.DTOs.Beneficiary;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;

namespace ABP.Core.Application.Interfaces.Services
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly IBeneficiaryRepository _repo;
        private readonly ISavingsAccountRepository _accountRepo;
        private readonly IMapper _mapper;

        public BeneficiaryService(IBeneficiaryRepository repo, ISavingsAccountRepository accountRepo, IMapper mapper)
        {
            _repo = repo;
            _accountRepo = accountRepo;
            _mapper = mapper;
        }

        public async Task<BeneficiaryDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<BeneficiaryDto>(entity);
        }

        public async Task<IEnumerable<BeneficiaryDto>> GetByOwnerIdAsync(string ownerId)
        {
            var entities = await _repo.GetByOwnerAccountIdAsync(ownerId);
            return _mapper.Map<IEnumerable<BeneficiaryDto>>(entities);
        }

        public async Task<bool> AddAsync(string ownerId, string accountNumber)
        {
            if (await _repo.BeneficiaryExistForOwnerAsync(ownerId, accountNumber))
                return false;

            var account = await _accountRepo.GetByAccountNumberAsync(accountNumber);
            if (account == null) return false;

            var beneficiary = new Beneficiary
            {
                AccountNumber = accountNumber,
                FirstName = string.Empty,
                LastName = string.Empty,
                OwnerId = ownerId
            };

            await _repo.AddAsync(beneficiary);
            return true;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity != null)
            {
                await _repo.DeleteAsync(entity);
            }
        }

        public async Task<bool> BeneficiaryExistsForOwnerAsync(string ownerId, string accountNumber)
        {
            return await _repo.BeneficiaryExistForOwnerAsync(ownerId, accountNumber);
        }
    }
}
