using ABP.API.DTOs.Account;
using ABP.Core.Application.Features.Account.Commands;
using ABP.Core.Application.DTOs.Account;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    public class AccountController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        /// <summary>
        /// Confirma y activa una cuenta de usuario mediante un token enviado por correo.
        /// </summary>
        /// <response code="204">Cuenta activada exitosamente.</response>
        /// <response code="400">El token es inválido o está vacío.</response>
        [HttpPost("confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Confirm([FromBody] ConfirmAccountRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Token))
            {
                return ApiProblem(400, "Token requerido", "El token es obligatorio.");
            }

            var result = await _mediator.Send(new ActivateAccountCommand(request.Token));
            if (!result)
            {
                return ApiProblem(400, "Token inválido", "El token no es válido o ya expiró.");
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
            return ApiProblem(403, "Acceso denegado", "No tienes permisos para acceder a este recurso.");
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
            var result = await _mediator.Send(new LoginCommand(request.UserName, request.Password));

            if (!result.Success)
                return ApiProblem(401, "Autenticación fallida", result.Error ?? "Las credenciales no son válidas.");

            return Ok(new { Jwt = result.JwtToken });
        }

        /// <summary>
        /// Solicita un token para restablecer la contraseña de un usuario.
        /// </summary>
        [HttpPost("get-reset-token")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenRequest request)
        {
            var result = await _mediator.Send(new GeneratePasswordResetTokenCommand(request.UserName, AccountEmailChannel.Api));

            if (!result)
                return ApiProblem(404, "Usuario no encontrado", "No existe un usuario con ese nombre.");

            return NoContent();
        }

        /// <summary>
        /// Cambia la contraseña del usuario utilizando un token de validación.
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return ApiProblem(400, "Contraseñas no coinciden", "La contraseña y su confirmación deben coincidir.");

            var result = await _mediator.Send(new ResetPasswordByUserIdCommand(
                request.UserId, request.Token, request.Password));

            if (!result)
                return ApiProblem(400, "Restablecimiento rechazado", "La contraseña no pudo restablecerse. Verifica el usuario y el token.");

            return NoContent();
        }
    }
}
