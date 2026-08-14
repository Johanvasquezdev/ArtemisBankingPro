using ABP.API.DTOs.Commerce;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/commerce")]
    [Authorize(Roles = "Admin")]
    public class CommerceApiController(ICommerceService commerceService) : BaseApiController
    {
        private readonly ICommerceService _commerceService = commerceService;

        // GET /api/v1/commerce
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string status = "activo")
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return BadRequest(new { message = "Parámetros de paginación inválidos." });

            if (status is not ("activo" or "inactivo" or "todos"))
                return BadRequest(new { message = "El estado debe ser activo, inactivo o todos." });

            var result = await _commerceService.GetAllPagedAsync(page, pageSize);

            var filtered = status switch
            {
                "activo" => result.Items.Where(c => c.IsActive),
                "inactivo" => result.Items.Where(c => !c.IsActive),
                _ => result.Items
            };

            return Ok(new
            {
                result.Page,
                result.PageSize,
                TotalRecords = filtered.Count(),
                Data = filtered
            });
        }

        // GET /api/v1/commerce/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var commerce = await _commerceService.GetByIdAsync(id);
            if (commerce == null) return NotFound(new { message = "El comercio especificado no existe." });
            return Ok(commerce);
        }

        // POST /api/v1/commerce
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCommerceRequest request)
        {
            var dto = new CommerceDto
            {
                Name = request.Name,
                Description = request.Description,
                Logo = request.Logo
            };

            await _commerceService.AddAsync(dto);
            return StatusCode(201, dto);
        }

        // PUT /api/v1/commerce/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommerceRequest request)
        {
            var existing = await _commerceService.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "El comercio especificado no existe." });

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Logo = request.Logo;

            await _commerceService.UpdateAsync(existing);
            return NoContent();
        }

        // PATCH /api/v1/commerce/{id}/status
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeCommerceStatusRequest request)
        {
            var existing = await _commerceService.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "El comercio especificado no existe." });

            await _commerceService.ChangeStatusAsync(id, request.Status);
            return NoContent();
        }
    }
}
