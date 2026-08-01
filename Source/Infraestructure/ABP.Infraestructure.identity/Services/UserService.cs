using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.identity.Entities;
using ABP.Infraestructure.Shared.EmailServices;
using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ABP.Infraestructure.identity.Services
{
    public class UserService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ICorreoServices emailServices, 
        ISavingsAccountService savingsAccountService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly ICorreoServices _emailService = emailServices;
        private readonly ISavingsAccountService _savingsAccountService = savingsAccountService;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IConfiguration _configuration = configuration;

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return Fail("The username or password are incorrect.");
            }

            if (!user.EmailConfirmed)
            {
                var passwordIsValid = await _userManager.CheckPasswordAsync(user, password);

                if (!passwordIsValid)
                {
                    return Fail("The username or password are incorrect.");
                }

                var confirmationEmailSent = await TryResendConfirmationEmailAsync(user);

                return confirmationEmailSent
                    ? Fail("Your account has not been confirmed yet. We sent you a new confirmation email.")
                    : Fail("Your account has not been confirmed yet. We could not send a new confirmation email right now.");
            }

            if (!user.IsActive)
            {
                return Fail("Your account is inactive. Please complete the pending email process or contact an administrator.");
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Fail("Invalid credentials.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            if (string.IsNullOrEmpty(roleName))
            {
                return Fail("The user has no assigned role.");
            }

            var role = Enum.Parse<UserRole>(roleName);

            return new AuthenticationResult
            {
                Success = true,
                UserId = user.Id,
                CommerceId = user.CommerceId ?? 0,
                UserName = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Role = role
            };
        }

        public async Task<bool> RegisterAsync(string firstName, string lastName, string cedula, string username, string email, string password, 
            string role, string adminId, decimal initialAmount = 0)
        {
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null) return false;

            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail != null) return false;

            var existingCedula = await _userManager.Users.AnyAsync(u => u.Cedula == cedula);
            if (existingCedula) return false;

            var parsedRole = Enum.Parse<UserRole>(role);

            var user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Cedula = cedula,
                UserName = username,
                Email = email,
                EmailConfirmed = false,
                IsActive = false,
                Role = parsedRole
            };

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, password);
            }
            catch (DbUpdateException)
            {
                return false;
            }

            if (!result.Succeeded) return false;

            if (parsedRole == UserRole.Client && initialAmount >= 0)
            {
                await _savingsAccountService.CreateAccountAsync(user.Id,adminId, initialAmount);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return false;
            }

            await SendActivationEmailAsync(user);
            return true;
        }

        public async Task<bool> RegisterCommerceUserAsync(string firstName, string lastName, string cedula, string username, string email, string password, int commerceId)
        {
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null) return false;

            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail != null) return false;

            var existingCedula = await _userManager.Users.AnyAsync(u => u.Cedula == cedula);
            if (existingCedula) return false;

            var user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Cedula = cedula,
                UserName = username,
                Email = email,
                EmailConfirmed = false,
                IsActive = false,
                Role = UserRole.Commerce,
                CommerceId = commerceId
            };

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(user, password);
            }
            catch (DbUpdateException)
            {
                return false;
            }

            if (!result.Succeeded) return false;

            await _userManager.AddToRoleAsync(user, UserRole.Commerce.ToString());

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.ActivationToken = token;
            await _userManager.UpdateAsync(user);

            await _emailService.SendEmailAsync(new EmailRequest
            {
                To = email,
                Subject = "ArtemisBank - Commerce Account Activation",
                Body = $"Your commerce account is ready. Use this token to activate: {token}"
            });

            return true;
        }

        public async Task<bool> ActivateAccountAsync(string token)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.ActivationToken == token);

            if (user == null) return false;

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded) return false;

            user.IsActive = true;
            user.ActivationToken = null;
            await _userManager.UpdateAsync(user);

            return true;
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public async Task<bool> GeneratePasswordResetTokenAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return false;

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            user.ActivationToken = token;
            await _userManager.UpdateAsync(user);

            await _emailService.SendEmailAsync(new EmailRequest
            {
                To = user.Email!,
                Subject = "Reset your ArtemisBank Password",
                Body = $"Click the following link to reset your password: {BuildResetPasswordLink(user.UserName!, token)}"
            });

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string username, string token, string newPassword)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return false;

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded) return false;

            user.IsActive = true;
            user.ActivationToken = null;
            await _userManager.UpdateAsync(user);

            return true;
        }

        public async Task<bool> ChangeStatusAsync(string adminId, string userId, bool isActive)
        {
            if (adminId == userId) return false;

            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || !await _userManager.IsInRoleAsync(admin, UserRole.Admin.ToString()))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = !isActive; //admin activated o deactivated manually

            if (isActive)
            {
                user.EmailConfirmed = true;
            }

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateAsync(UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null) return false;

            var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingEmail != null && existingEmail.Id != dto.Id) return false;

            var existingUser = await _userManager.FindByNameAsync(dto.Username);
            if (existingUser != null && existingUser.Id != dto.Id) return false;

            var existingCedula = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Cedula == dto.Cedula);
            if (existingCedula != null && existingCedula.Id != dto.Id) return false;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Cedula = dto.Cedula;
            user.Email = dto.Email;
            user.UserName = dto.Username;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, dto.Password);
                if (!passwordResult.Succeeded) return false;
            }

            IdentityResult result;
            try
            {
                result = await _userManager.UpdateAsync(user);
            }
            catch (DbUpdateException)
            {
                return false;
            }

            return result.Succeeded;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
        
        #region private methods
        private static AuthenticationResult Fail(string error) =>
            new() { Success = false, Error = error };

        private async Task SendActivationEmailAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.ActivationToken = token;
            await _userManager.UpdateAsync(user);

            await _emailService.SendAsync(
                user.Email!,
                "Activa tu cuenta - Artemis Banking",
                $"Hola {user.FirstName}, haz clic en el siguiente enlace para activar tu cuenta: {BuildActivationLink(token)}");
        }

        private async Task<bool> TryResendConfirmationEmailAsync(ApplicationUser user)
        {
            var previousActivationToken = user.ActivationToken;

            try
            {
                await SendActivationEmailAsync(user);
                return true;
            }
            catch
            {
                user.ActivationToken = previousActivationToken;

                try
                {
                    await _userManager.UpdateAsync(user);
                }
                catch
                {
                    // Ignore rollback failures so the login flow can return a controlled message.
                }

                return false;
            }
        }

        private string BuildActivationLink(string token)
        {
            var encodedToken = Uri.EscapeDataString(token);
            return $"{ResolveBaseUrl()}/Account/Activate?token={encodedToken}";
        }

        private string BuildResetPasswordLink(string username, string token)
        {
            var encodedUsername = Uri.EscapeDataString(username);
            var encodedToken = Uri.EscapeDataString(token);
            return $"{ResolveBaseUrl()}/Login/ResetPassword?username={encodedUsername}&token={encodedToken}";
        }

        private string ResolveBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request?.Host.HasValue == true)
            {
                return $"{request.Scheme}://{request.Host.Value}";
            }

            var configuredUrl = _configuration["ApplicationUrl"];
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl.TrimEnd('/');
            }

            return "https://localhost:7108";
        }
        #endregion
    }
}
