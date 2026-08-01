using ABP.Core.Application.DTOs.User;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IUserService
    {
        // Authentication
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);

        // Registration
        Task<bool> RegisterAsync(string firstName, string lastName, string cedula, string username, 
            string email, string password, string role, string adminId, decimal initialAmount = 0);
        Task<bool> RegisterCommerceUserAsync(string firstName, string lastName, string cedula, string username, string email, string password, int commerceId);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task<bool> ActivateAccountAsync(string token);

        // Password
        Task<bool> GeneratePasswordResetTokenAsync(string username);
        Task<bool> ResetPasswordAsync(string username, string token, string newPassword);

        Task<bool> UpdateAsync(UpdateUserDto dto);
        Task<bool> ChangeStatusAsync(string adminId, string userId, bool isActive);
        Task LogoutAsync();
    }
}
