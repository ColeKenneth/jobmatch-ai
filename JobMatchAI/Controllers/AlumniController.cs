using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    public class AlumniController(IAlumniService alumniService) : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<AlumniDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<AlumniDto>>> GetAll()
        {
            var alumni = await alumniService.GetAllAsync();
            return Ok(alumni);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AlumniDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AlumniDto>> GetById(Guid id)
        {
            var alumnus = await alumniService.GetByIdAsync(id);
            return alumnus is null ? NotFound() : Ok(alumnus);
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(AlumniDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AlumniDto>> GetByUserId(Guid userId)
        {
            var alumnus = await alumniService.GetByUserIdAsync(userId);
            return alumnus is null ? NotFound() : Ok(alumnus);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Alumni")]
        [ProducesResponseType(typeof(AlumniDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AlumniDto>> Create([FromBody] AlumniDto dto)
        {
            var created = await alumniService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Alumni")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] AlumniDto dto)
        {
            await alumniService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Alumni")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await alumniService.DeleteAsync(id);
            return NoContent();
        }
    }
}
