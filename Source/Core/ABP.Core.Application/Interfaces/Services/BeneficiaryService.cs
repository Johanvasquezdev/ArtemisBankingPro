using AutoMapper;
using ABP.Core.Application.DTOs.Beneficiary;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Exceptions;
using ABP.Core.Domain.Interfaces;

namespace ABP.Core.Application.Interfaces.Services
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly IBeneficiaryRepository _repo;
        private readonly ISavingsAccountRepository _accountRepo;
        private readonly IUserReadOnlyService _userService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork? _unitOfWork;

        public BeneficiaryService(
            IBeneficiaryRepository repo,
            ISavingsAccountRepository accountRepo,
            IUserReadOnlyService userService,
            IMapper mapper,
            IUnitOfWork? unitOfWork = null)
        {
            _repo = repo;
            _accountRepo = accountRepo;
            _userService = userService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
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

        public async Task<BeneficiaryDto> AddAsync(string ownerId, string accountNumber)
        {
            if (await _repo.BeneficiaryExistForOwnerAsync(ownerId, accountNumber))
                throw new DuplicateBeneficiaryException();

            var account = await _accountRepo.GetByAccountNumberAsync(accountNumber)
                ?? throw new InvalidAccountException();

            if (account.Status == AccountStatus.Closed)
                throw new ClosedAccountException();

            if (account.Status != AccountStatus.Active)
                throw new InvalidAccountException();

            if (account.UserId == ownerId)
                throw new OwnAccountException();

            var owner = await _userService.GetByIdAsync(account.UserId);

            var beneficiary = new Beneficiary
            {
                AccountNumber = accountNumber,
                FirstName = owner?.FirstName ?? string.Empty,
                LastName = owner?.LastName ?? string.Empty,
                OwnerId = ownerId
            };

            await _repo.AddAsync(beneficiary);
            if (_unitOfWork is not null)
                await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<BeneficiaryDto>(beneficiary);
        }

        public async Task DeleteAsync(int id, string ownerId)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return;
            if (entity.OwnerId != ownerId) return;

            await _repo.DeleteAsync(entity);
            if (_unitOfWork is not null)
                await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> BeneficiaryExistsForOwnerAsync(string ownerId, string accountNumber)
        {
            return await _repo.BeneficiaryExistForOwnerAsync(ownerId, accountNumber);
        }
    }
}
