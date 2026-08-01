using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.identity.Services
{
    public class UserReadOnlyService(UserManager<ApplicationUser> userManager) : IUserReadOnlyService
    {
        #region private fields
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        #endregion

        public async Task<UserDto> GetByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null!;

            var roles = await _userManager.GetRolesAsync(user);

            return MapToDto(user, roles.FirstOrDefault());
        }

        public async Task<bool> ExistsByCedulaAsync(string cedula, string? excludingUserId = null)
        {
            return await _userManager.Users.AnyAsync(u =>
                u.Cedula == cedula &&
                (excludingUserId == null || u.Id != excludingUserId));
        }

        public async Task<string?> GetActivationTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.ActivationToken;
        }

        public async Task<int> GetInactiveClientsCountAsync()
        {
            return await _userManager.Users.CountAsync(u => u.Role == UserRole.Client && !u.IsActive);
        }

        public async Task<int> GetActiveClientsCountAsync()
        {
            return await _userManager.Users.CountAsync(u => u.Role == UserRole.Client && u.IsActive);
        }

        public async Task<IEnumerable<UserDto>> GetActiveClientsAsync(string? cedula = null)
        {
            var query = _userManager.Users.Where(u => u.Role == UserRole.Client && u.IsActive);

            if (!string.IsNullOrEmpty(cedula)) query = query.Where(u => u.Cedula.Contains(cedula));

            var clients = await query.ToListAsync();

            return clients.Select(u => MapToDto(u, UserRole.Client.ToString()));
        }

        public async Task<PaginatedResult<UserDto>> GetAllAsync(int page, int pageSize = 20, UserRole? role = null)
        {
            var query = _userManager.Users.Where(u => u.Role != UserRole.Commerce).OrderByDescending(u => u.Id.CompareTo(""));

            if (role.HasValue)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Value.ToString());
                var userIds = usersInRole.Select(u => u.Id).ToHashSet();
                query = (IOrderedQueryable<ApplicationUser>)query.Where(u => u.Role == role.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query.OrderBy(u => u.UserName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToDto(user, roles.FirstOrDefault()));
            }

            return new PaginatedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PaginatedResult<UserDto>> GetCommerceUsersAsync(int page, int pageSize = 20)
        {
            var query = _userManager.Users
                .Where(u => u.Role == UserRole.Commerce)
                .OrderByDescending(u => u.Id);

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToDto(user, roles.FirstOrDefault()));
            }

            return new PaginatedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        #region private methods
        private static UserDto MapToDto(ApplicationUser user, string? roleName) =>
            new()
            {
                Id = user.Id,
                UserName = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Cedula = user.Cedula,
                Email = user.Email!,
                Role = string.IsNullOrEmpty(roleName) ? UserRole.Client : Enum.Parse<UserRole>(roleName),
                IsActive = user.IsActive
            };
        #endregion
    }
}
