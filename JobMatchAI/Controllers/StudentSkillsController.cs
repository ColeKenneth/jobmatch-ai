using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentSkillsController(IStudentSkillService studentSkillService) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(StudentSkillDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentSkillDto>> GetById(Guid id)
        {
            var studentSkill = await studentSkillService.GetByIdAsync(id);
            return studentSkill is null ? NotFound() : Ok(studentSkill);
        }

        [HttpGet("student/{studentId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<StudentSkillDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<StudentSkillDto>>> GetByStudent(Guid studentId)
        {
            var studentSkills = await studentSkillService.GetByStudentAsync(studentId);
            return Ok(studentSkills);
        }

        [HttpPost("student/{studentId:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(typeof(StudentSkillDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<StudentSkillDto>> AddSkill(Guid studentId, [FromBody] StudentSkillDto dto)
        {
            var created = await studentSkillService.AddSkillAsync(studentId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] StudentSkillDto dto)
        {
            await studentSkillService.UpdateSkillAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveSkill(Guid id)
        {
            await studentSkillService.RemoveSkillAsync(id);
            return NoContent();
        }

        [HttpDelete("student/{studentId:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveAllForStudent(Guid studentId)
        {
            await studentSkillService.RemoveAllForStudentAsync(studentId);
            return NoContent();
        }
    }
}
