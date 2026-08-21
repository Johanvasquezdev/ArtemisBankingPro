using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.identity.Entities;
using ABP.Infraestructure.Shared.EmailServices;
using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace ABP.Infraestructure.identity.Services
{
    public class UserService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ICorreoServices emailServices,
        ISavingsAccountService savingsAccountService, ICommerceService commerceService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly ICorreoServices _emailService = emailServices;
        private readonly ISavingsAccountService _savingsAccountService = savingsAccountService;
        private readonly ICommerceService _commerceService = commerceService;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IConfiguration _configuration = configuration;

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return Fail("El usuario o la contraseña son incorrectos.");
            }

            if (!user.EmailConfirmed)
            {
                var passwordIsValid = await _userManager.CheckPasswordAsync(user, password);

                if (!passwordIsValid)
                {
                    return Fail("El usuario o la contraseña son incorrectos.");
                }

                var confirmationEmailSent = await TryResendConfirmationEmailAsync(user);

                return confirmationEmailSent
                    ? Fail("Tu cuenta aún no ha sido confirmada. Te enviamos un nuevo correo de confirmación.")
                    : Fail("Tu cuenta aún no ha sido confirmada. No pudimos enviar un nuevo correo de confirmación en este momento.");
            }

            if (!user.IsActive)
            {
                return Fail("Tu cuenta está inactiva. Completa el proceso pendiente por correo o contacta a un administrador.");
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Fail("Credenciales inválidas.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault();

            if (string.IsNullOrEmpty(roleName))
            {
                return Fail("El usuario no tiene un rol asignado.");
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
            string role, string adminId, decimal initialAmount = 0, AccountEmailChannel emailChannel = AccountEmailChannel.Web)
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

            if (!result.Succeeded)
            {
                return false;
            }

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

            await SendActivationEmailAsync(user, emailChannel);
            return true;
        }

        public async Task<bool> RegisterCommerceUserAsync(string firstName, string lastName, string cedula, string username, string email, string password, int commerceId, AccountEmailChannel emailChannel = AccountEmailChannel.Api)
        {
            var commerce = await _commerceService.GetByIdAsync(commerceId);
            if (commerce is null || !commerce.IsActive) return false;

            var alreadyAssociated = await _userManager.Users.AnyAsync(user =>
                user.Role == UserRole.Commerce && user.CommerceId == commerceId);
            if (alreadyAssociated) return false;

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

            if (!result.Succeeded)
            {
                return false;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, UserRole.Commerce.ToString());
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return false;
            }

            try
            {
                // Every commerce needs a settlement account for Hermes Pay deposits.
                await _savingsAccountService.CreateAccountAsync(user.Id, "SYSTEM", 0);
            }
            catch
            {
                await _userManager.DeleteAsync(user);
                return false;
            }

            await SendActivationEmailAsync(user, emailChannel);

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

        public async Task<bool> GeneratePasswordResetTokenAsync(string username, AccountEmailChannel emailChannel = AccountEmailChannel.Web)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return false;

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            user.ActivationToken = token;
            await _userManager.UpdateAsync(user);

            var resetBody = emailChannel == AccountEmailChannel.Web
                ? $"<div style=\"text-align: center;\"><p>Haga clic en el bot&oacute;n de abajo para restablecer su contrase&ntilde;a:</p><a href=\"{BuildResetPasswordLink(user.UserName!, token)}\" style=\"display: inline-block; padding: 12px 24px; background-color: #c5a059; color: #141414; text-decoration: none; border-radius: 4px; font-weight: bold; margin-top: 16px;\">Restablecer Contrase&ntilde;a</a></div>"
                : $"<div style=\"text-align: center;\"><p>Has solicitado restablecer tu contrase&ntilde;a mediante la API.</p><p>Utiliza los siguientes datos para completar el proceso:</p><div style=\"background-color: #151515; color: #ffffff; padding: 16px; border-radius: 8px; text-align: left; margin: 16px auto; max-width: 400px; word-break: break-all;\"><strong>UserId:</strong> {user.Id}<br><br><strong>Token:</strong> {token}</div></div>";

            await _emailService.SendEmailAsync(new EmailRequest
            {
                To = user.Email!,
                Subject = "Restablecer su contraseña de ArtemisBank",
                Body = resetBody,
                IsHtml = true
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

            if (user.Role == UserRole.Commerce && isActive)
            {
                if (!user.CommerceId.HasValue)
                    return false;

                var commerce = await _commerceService.GetByIdAsync(user.CommerceId.Value);
                if (commerce is null || !commerce.IsActive)
                    return false;

                var anotherActiveUser = await _userManager.Users.AnyAsync(candidate =>
                    candidate.Id != user.Id &&
                    candidate.Role == UserRole.Commerce &&
                    candidate.CommerceId == user.CommerceId &&
                    candidate.IsActive);
                if (anotherActiveUser)
                    return false;
            }

            user.IsActive = isActive;

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

            // An additional amount is a deposit into the client's existing
            // primary account. It must not silently create a financial
            // product from the identity-edit screen.
            var additionalAmount = dto.AdditionalAmount.GetValueOrDefault();
            var primaryAccount = additionalAmount > 0
                ? await _savingsAccountService.GetPrimaryAccountByClientIdAsync(user.Id)
                : null;

            if (additionalAmount > 0 && (primaryAccount is null || primaryAccount.Status != AccountStatus.Active))
            {
                throw new InvalidOperationException(
                    "No se puede agregar el monto porque el cliente no tiene una cuenta principal de ahorro activa. Asigne primero una cuenta de ahorro principal.");
            }

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

            if (!result.Succeeded)
            {
                return false;
            }

            if (additionalAmount > 0 && primaryAccount is not null)
            {
                var deposited = await _savingsAccountService.DepositAsync(
                    primaryAccount.AccountNumber,
                    additionalAmount);

                if (!deposited)
                {
                    throw new InvalidOperationException(
                        "El usuario se actualizó, pero no fue posible acreditar el monto adicional en su cuenta principal.");
                }
            }

            return true;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task DeactivateUsersByCommerceIdAsync(int commerceId)
        {
            var associatedUsers = await _userManager.Users
                .Where(u => u.CommerceId == commerceId && u.IsActive)
                .ToListAsync();

            foreach (var user in associatedUsers)
            {
                user.IsActive = false;
                await _userManager.UpdateAsync(user);
            }
        }

        #region private methods
        private static AuthenticationResult Fail(string error) =>
            new() { Success = false, Error = error };

        private async Task SendActivationEmailAsync(ApplicationUser user, AccountEmailChannel emailChannel)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            user.ActivationToken = token;
            await _userManager.UpdateAsync(user);

            var activationLink = BuildActivationLink(token);
            var isWeb = emailChannel == AccountEmailChannel.Web;

            await _emailService.SendEmailAsync(new EmailRequest
            {
                To = user.Email!,
                Subject = "Tu acceso a Artemis Banking Pro está listo",
                Body = isWeb ? BuildActivationEmailHtml(user, activationLink) : BuildApiTokenEmail(user, token),
                TextBody = isWeb ? BuildActivationEmailText(user, activationLink) : BuildApiTokenEmail(user, token),
                IsHtml = isWeb
            });
        }

        private static string BuildApiTokenEmail(ApplicationUser user, string token) =>
            $"Artemis Banking Pro API\n\nTu cuenta está lista. Usa este token en POST /api/v1/Account/confirm.\n\nUserId: {user.Id}\nToken: {token}\n\nNo compartas este token.";

        private static string BuildActivationEmailHtml(ApplicationUser user, string activationLink)
        {
            var firstName = WebUtility.HtmlEncode(user.FirstName);
            var username = WebUtility.HtmlEncode(user.UserName);
            var role = WebUtility.HtmlEncode(GetRoleLabel(user.Role));
            var safeActivationLink = WebUtility.HtmlEncode(activationLink);

            return $$"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Tu acceso a Artemis Banking Pro</title>
</head>
<body style="margin:0; padding:0; background:#f5f3ee; color:#141414; font-family:Georgia,'Times New Roman',serif;">
  <span style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">Tu cuenta está lista. Activa tu acceso seguro a Artemis Banking Pro.</span>
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f5f3ee; padding:32px 12px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px; background:#ffffff; border:1px solid #e6dfd2; border-radius:16px; overflow:hidden;">
          <tr>
            <td style="padding:30px 38px; background:#151515;">
              <div style="font-family:Georgia,'Times New Roman',serif; font-size:25px; line-height:1.1; color:#ffffff;">Artemis <span style="color:#c5a059; font-style:italic;">Banking</span></div>
              <div style="margin-top:8px; color:#dfc48c; font-size:11px; letter-spacing:3px;">PRIVATE WEALTH</div>
            </td>
          </tr>
          <tr>
            <td style="padding:42px 38px 36px;">
              <div style="display:inline-block; padding:8px 12px; border-radius:999px; background:#fbf6e9; color:#9c7b3e; font-size:11px; font-weight:bold; letter-spacing:1.5px;">CUENTA CREADA</div>
              <h1 style="margin:22px 0 12px; font-family:Georgia,'Times New Roman',serif; font-size:34px; line-height:1.15; font-weight:normal; color:#141414;">Bienvenido, {{firstName}}</h1>
              <p style="margin:0; color:#5f625f; font-size:16px; line-height:1.65;">Tu cuenta de Artemis Banking Pro está lista. Activa tus credenciales para comenzar a administrar tus productos financieros de forma segura.</p>

              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:28px 0; border:1px solid #ece7dd; border-radius:10px; background:#faf9f6;">
                <tr>
                  <td style="padding:16px 18px; border-bottom:1px solid #ece7dd; color:#77736b; font-size:12px; letter-spacing:1px;">USUARIO</td>
                  <td align="right" style="padding:16px 18px; border-bottom:1px solid #ece7dd; color:#141414; font-size:14px; font-weight:bold;">{{username}}</td>
                </tr>
                <tr>
                  <td style="padding:16px 18px; color:#77736b; font-size:12px; letter-spacing:1px;">PERFIL</td>
                  <td align="right" style="padding:16px 18px; color:#9c7b3e; font-size:14px; font-weight:bold;">{{role}}</td>
                </tr>
              </table>

              <p style="margin:0 0 22px; color:#5f625f; font-size:15px; line-height:1.6;">Cuando estés listo, utiliza el botón para confirmar tu correo y habilitar el acceso.</p>
              <table role="presentation" cellspacing="0" cellpadding="0">
                <tr>
                  <td style="border-radius:8px; background:#c5a059;">
                    <a href="{{safeActivationLink}}" style="display:inline-block; padding:15px 24px; color:#141414; font-size:15px; font-weight:bold; text-decoration:none;">Activar mi cuenta&nbsp; →</a>
                  </td>
                </tr>
              </table>

              <div style="margin-top:30px; padding:16px 18px; border:1px solid #e6d7b7; border-radius:8px; background:#fbf8ee; color:#67645d; font-size:13px; line-height:1.6;">Este enlace es personal y de un solo uso. Si no reconoces esta solicitud, puedes ignorar este mensaje.</div>
            </td>
          </tr>
          <tr>
            <td style="padding:22px 38px; border-top:1px solid #eeeae2; background:#faf9f6; color:#88847b; font-size:12px; line-height:1.6;">Artemis Banking Pro · Private Wealth<br>Este mensaje fue enviado automáticamente; por favor, no respondas a este correo.</td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
        }

        private static string BuildActivationEmailText(ApplicationUser user, string activationLink) =>
            $"Hola {user.FirstName},\n\nTu cuenta de Artemis Banking Pro está lista. Activa tus credenciales para comenzar.\n\nUsuario: {user.UserName}\nPerfil: {GetRoleLabel(user.Role)}\n\nActiva tu cuenta aquí:\n{activationLink}\n\nEste enlace es personal y de un solo uso. Si no reconoces esta solicitud, puedes ignorar este mensaje.\n\nArtemis Banking Pro · Private Wealth";

        private static string GetRoleLabel(UserRole role) => role switch
        {
            UserRole.Client => "Cliente",
            UserRole.Commerce => "Comercio",
            UserRole.Cashier => "Cajero",
            UserRole.Admin => "Administrador",
            _ => role.ToString()
        };

        private async Task<bool> TryResendConfirmationEmailAsync(ApplicationUser user)
        {
            var previousActivationToken = user.ActivationToken;

            try
            {
                await SendActivationEmailAsync(user, AccountEmailChannel.Web);
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










