using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobSkillsController(IJobSkillService jobSkillService) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(JobSkillDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JobSkillDto>> GetById(Guid id)
        {
            var jobSkill = await jobSkillService.GetByIdAsync(id);
            return jobSkill is null ? NotFound() : Ok(jobSkill);
        }

        [HttpGet("job/{jobPostingId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<JobSkillDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobSkillDto>>> GetByJobPosting(Guid jobPostingId)
        {
            var jobSkills = await jobSkillService.GetByJobPostingAsync(jobPostingId);
            return Ok(jobSkills);
        }

        [HttpPost("job/{jobPostingId:guid}")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(typeof(JobSkillDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<JobSkillDto>> AddSkill(Guid jobPostingId, [FromBody] JobSkillDto dto)
        {
            var created = await jobSkillService.AddSkillAsync(jobPostingId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] JobSkillDto dto)
        {
            await jobSkillService.UpdateSkillAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveSkill(Guid id)
        {
            await jobSkillService.RemoveSkillAsync(id);
            return NoContent();
        }

        [HttpDelete("jobPosting/{jobPostingId:guid}")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveAllForJobPosting(Guid jobPostingId)
        {
            await jobSkillService.RemoveAllForJobPostingAsync(jobPostingId);
            return NoContent();
        }

    }
}