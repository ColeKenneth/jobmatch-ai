using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployersController(IEmployerService employerService) : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<EmployerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<EmployerDto>>> GetAll()
        {
            var employers = await employerService.GetAllAsync();
            return Ok(employers);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(EmployerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployerDto>> GetById(Guid id)
        {
            var employer = await employerService.GetByIdAsync(id);
            return employer is null ? NotFound() : Ok(employer);
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(EmployerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployerDto>> GetByUserId(Guid userId)
        {
            var employer = await employerService.GetByUserIdAsync(userId);
            return employer is null ? NotFound() : Ok(employer);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(typeof(EmployerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<EmployerDto>> Create([FromBody] EmployerDto dto)
        {
            var created = await employerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] EmployerDto dto)
        {
            await employerService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await employerService.DeleteAsync(id);
            return NoContent();
        }
    }
}
