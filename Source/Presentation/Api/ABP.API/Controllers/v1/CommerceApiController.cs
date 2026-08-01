using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using ABP.API.DTOs.Commerce;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin")]
    [Route("api/v{version:apiVersion}/commerce")]
    public class CommerceApiController : BaseApiController
    {
        private readonly ICommerceService _commerceService;
        private readonly IUserService _userService;

        public CommerceApiController(
            ICommerceService commerceService,
            IUserService userService)
        {
            _commerceService = commerceService;
            _userService = userService;
        }

        /// <summary>
        /// Obtiene todos los comercios de forma paginada o completa.
        /// </summary>
        /// <param name="page">Número de página (opcional)</param>
        /// <param name="pageSize">Tamaño de página (opcional)</param>
        /// <response code="200">Listado retornado exitosamente</response>
        /// <response code="401">Token ausente o inválido</response>
        /// <response code="403">Usuario sin permisos de administrador</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCommerces(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null)
        {
            if (page.HasValue && pageSize.HasValue)
            {
                var pagedResult = await _commerceService.GetAllPagedAsync(page.Value, pageSize.Value);
                return Ok(pagedResult);
            }

            var allCommerces = await _commerceService.GetAllAsync();
            return Ok(allCommerces);
        }

        /// <summary>
        /// Obtiene un comercio por su ID.
        /// </summary>
        /// <param name="id">ID del comercio</param>
        /// <response code="200">Comercio encontrado</response>
        /// <response code="404">Comercio no encontrado</response>
        /// <response code="401">Token ausente o inválido</response>
        /// <response code="403">Usuario sin permisos de administrador</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCommerceById([FromRoute] int id)
        {
            var commerce = await _commerceService.GetByIdAsync(id);

            if (commerce == null)
                return NotFound(new { message = "Commerce not found." });

            return Ok(commerce);
        }

        /// <summary>
        /// Crea un nuevo comercio.
        /// </summary>
        /// <param name="request">Datos del comercio</param>
        /// <response code="201">Comercio creado exitosamente</response>
        /// <response code="400">Datos faltantes o inválidos</response>
        /// <response code="401">Token ausente o inválido</response>
        /// <response code="403">Usuario sin permisos de administrador</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateCommerce([FromBody] CreateCommerceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var commerceDto = new CommerceDto
            {
                Name = request.Name,
                Description = request.Description,
                Logo = request.Logo,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _commerceService.AddAsync(commerceDto);

            return StatusCode(201, new { message = "Commerce created successfully." });
        }

        /// <summary>
        /// Actualiza un comercio existente.
        /// </summary>
        /// <param name="id">ID del comercio</param>
        /// <param name="request">Datos actualizados</param>
        /// <response code="204">Comercio actualizado</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="404">Comercio no encontrado</response>
        /// <response code="401">Token ausente o inválido</response>
        /// <response code="403">Usuario sin permisos de administrador</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateCommerce(
            [FromRoute] int id,
            [FromBody] UpdateCommerceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var commerce = await _commerceService.GetByIdAsync(id);
            if (commerce == null)
                return NotFound(new { message = "Commerce not found." });

            var commerceDto = new CommerceDto
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                Logo = request.Logo,
                IsActive = commerce.IsActive,
                CreatedAt = commerce.CreatedAt
            };

            await _commerceService.UpdateAsync(commerceDto);

            return NoContent();
        }

        /// <summary>
        /// Cambia el estado de un comercio (activo/inactivo).
        /// Al desactivar un comercio, se desactivan sus usuarios asociados.
        /// </summary>
        /// <param name="id">ID del comercio</param>
        /// <param name="request">Estado a asignar</param>
        /// <response code="204">Estado cambiado</response>
        /// <response code="404">Comercio no encontrado</response>
        /// <response code="401">Token ausente o inválido</response>
        /// <response code="403">Usuario sin permisos de administrador</response>
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangeCommerceStatus(
            [FromRoute] int id,
            [FromBody] ChangeCommerceStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var commerce = await _commerceService.GetByIdAsync(id);
            if (commerce == null)
                return NotFound(new { message = "Commerce not found." });

            await _commerceService.ChangeStatusAsync(id, request.Status);

            return NoContent();
        }
    }
}
