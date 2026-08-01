using ABP.Core.Application.Interfaces.IServices;
using ABP.API.DTOs.Account;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    public class AccountController(IUserService userService, IUserReadOnlyService userReadOnlyService, IJwtService jwtService) : BaseApiController
    {
        private readonly IUserService _userService = userService;
        private readonly IUserReadOnlyService _userReadOnlyService = userReadOnlyService;
        private readonly IJwtService _jwtService = jwtService;

        /// <summary>
        /// Confirma y activa una cuenta de usuario mediante un token enviado por correo.
        /// </summary>
        /// <response code="204">Cuenta activada exitosamente.</response>
        /// <response code="400">El token es inválido o está vacío.</response>
        [Authorize]
        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Confirm([FromBody] ConfirmAccountRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { message = "Token is required." });
            }

            var result = await _userService.ActivateAccountAsync(request.Token);
            if (!result)
            {
                return BadRequest(new { message = "Invalid token." });
            }

            return NoContent();
        }

        /// <summary>
        /// Endpoint auxiliar para manejar denegación de permisos.
        /// </summary>
        [HttpGet("access-denied")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult AccessDenied()
        {
            return StatusCode(403, new { message = "You do not have permission to access this resource." });
        }

        /// <summary>
        /// Autentica a un usuario y genera un token JWT de acceso.
        /// </summary>
        /// <param name="request">Credenciales de acceso (Usuario y Contraseña).</param>
        /// <returns>Un token JWT válido por tiempo limitado.</returns>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Password and user are required." });

            var result = await _userService.AuthenticateAsync(request.UserName, request.Password);

            if (!result.Success)
                return Unauthorized(new { message = result.Error });

            var token = await _jwtService.GenerateTokenAsync(
                result.UserId,
                result.UserName,
                result.Email,
                new[] { result.Role.ToString() },
                result.CommerceId);

            return Ok(new { Jwt = token });
        }

        /// <summary>
        /// Solicita un token para restablecer la contraseña de un usuario.
        /// </summary>
        [Authorize]
        [HttpPost("get-reset-token")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName))
                return BadRequest(new { message = "The username is required." });

            var result = await _userService.GeneratePasswordResetTokenAsync(request.UserName);

            if (!result)
                return BadRequest(new { message = "This user doesn`t exist." });

            return NoContent();
        }

        /// <summary>
        /// Cambia la contraseña del usuario utilizando un token de validación.
        /// </summary>
        [Authorize]
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Token) ||
                string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.ConfirmPassword))
                return BadRequest(new { message = "All fields are required." });

            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "The passwords do not match." });

            var user = await _userReadOnlyService.GetByIdAsync(request.UserId);
            if (user == null || string.IsNullOrWhiteSpace(user.UserName))
                return BadRequest(new { message = "The user is invalid." });

            var result = await _userService.ResetPasswordAsync(
                user.UserName, request.Token, request.Password);

            if (!result)
                return BadRequest(new { message = "The password could not be reset." });

            return NoContent();
        }
    }
}