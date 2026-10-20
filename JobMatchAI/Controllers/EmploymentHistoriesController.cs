using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmploymentHistoriesController(IEmploymentHistoryService employmentHistoryService) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EmploymentHistoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmploymentHistoryDto>> GetById(Guid id)
        {
            var history = await employmentHistoryService.GetByIdAsync(id);
            return history is null ? NotFound() : Ok(history);
        }

        [HttpGet("alumni/{alumniId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EmploymentHistoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EmploymentHistoryDto>>> GetByAlumni(Guid alumniId)
        {
            var histories = await employmentHistoryService.GetByAlumniAsync(alumniId);
            return Ok(histories);
        }

        [HttpPost("alumni/{alumniId:guid}")]
        [Authorize(Roles = "Admin,Alumni")]
        [ProducesResponseType(typeof(EmploymentHistoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<EmploymentHistoryDto>> Create(Guid alumniId, [FromBody] EmploymentHistoryDto dto)
        {
            var created = await employmentHistoryService.CreateAsync(alumniId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Alumni")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] EmploymentHistoryDto dto)
        {
            await employmentHistoryService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Alumni")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await employmentHistoryService.DeleteAsync(id);
            return NoContent();
        }
    }
}