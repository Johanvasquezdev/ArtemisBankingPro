using AutoMapper;
using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Core.Domain.Exceptions;

namespace ABP.Core.Application.Interfaces.Services
{
    public class CommerceService : ICommerceService
    {
        private readonly ICommerceRepository _repo;
        private readonly IMapper _mapper;
        private readonly ICommerceUserDirectory _commerceUsers;
        private readonly IUnitOfWork _unitOfWork;

        public CommerceService(ICommerceRepository repo, IMapper mapper, ICommerceUserDirectory commerceUsers, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _mapper = mapper;
            _commerceUsers = commerceUsers;
            _unitOfWork = unitOfWork;
        }

        public async Task<CommerceDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<CommerceDto>(entity);
        }

        public Task<string?> GetActiveUserIdAsync(int commerceId)
            => _commerceUsers.GetActiveUserIdAsync(commerceId);

        public async Task<IEnumerable<CommerceDto>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<CommerceDto>>(entities.Where(e => e.IsActive));
        }

        public async Task<PaginatedResult<CommerceDto>> GetAllPagedAsync(int page, int pageSize = 20, bool? isActive = null)
        {
            var entities = await _repo.GetAllPagedAsync(page, pageSize, isActive);
            var items = _mapper.Map<IEnumerable<CommerceDto>>(entities);
            var all = (await _repo.GetAllAsync()).Where(c => !isActive.HasValue || c.IsActive == isActive.Value);
            var totalCount = all.Count();

            return new PaginatedResult<CommerceDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CommerceDto> AddAsync(CommerceDto dto)
        {
            Normalize(dto);
            if (await _repo.ExistsByRncAsync(dto.Rnc))
                throw new DuplicateCommerceException("Ya existe un comercio registrado con este RNC.");
            if (await _repo.ExistsByEmailAsync(dto.Email))
                throw new DuplicateCommerceException("Ya existe un comercio registrado con este correo electrónico.");

            var entity = _mapper.Map<Commerce>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;
            await _repo.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CommerceDto>(entity);
        }

        public async Task UpdateAsync(CommerceDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return;

            Normalize(dto);
            if (await _repo.ExistsByRncAsync(dto.Rnc, dto.Id))
                throw new DuplicateCommerceException("Ya existe un comercio registrado con este RNC.");
            if (await _repo.ExistsByEmailAsync(dto.Email, dto.Id))
                throw new DuplicateCommerceException("Ya existe un comercio registrado con este correo electrónico.");
            entity.Rnc = dto.Rnc;
            entity.Email = dto.Email;
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.Logo = dto.Logo;
            entity.IsActive = dto.IsActive;
            await _repo.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangeStatusAsync(int id, bool isActive)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return;

            entity.IsActive = isActive;
            await _repo.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity != null)
            {
                await _repo.DeleteAsync(entity);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public Task<bool> HasActiveUserAsync(int commerceId)
            => _commerceUsers.HasActiveUserAsync(commerceId);

        private static void Normalize(CommerceDto dto)
        {
            dto.Name = dto.Name.Trim();
            dto.Description = dto.Description.Trim();
            dto.Rnc = new string(dto.Rnc.Where(char.IsDigit).ToArray());
            dto.Email = dto.Email.Trim().ToLowerInvariant();
            dto.Logo = dto.Logo.Trim();
        }
    }
}
