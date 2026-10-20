using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchResultsController(IMatchResultService matchResultService) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MatchResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MatchResultDto>> GetById(Guid id)
        {
            var result = await matchResultService.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("student/{studentId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<MatchResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MatchResultDto>>> GetByStudent(Guid studentId)
        {
            var results = await matchResultService.GetByStudentAsync(studentId);
            return Ok(results);
        }

        [HttpGet("job/{jobPostingId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<MatchResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MatchResultDto>>> GetByJobPosting(Guid jobPostingId)
        {
            var results = await matchResultService.GetByJobPostingAsync(jobPostingId);
            return Ok(results);
        }

        [HttpGet("student/{studentId:guid}/job/{jobPostingId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MatchResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MatchResultDto>> GetByStudentAndJob(Guid studentId, Guid jobPostingId)
        {
            var result = await matchResultService.GetByStudentAndJobAsync(studentId, jobPostingId);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Student,Employer")]
        [ProducesResponseType(typeof(MatchResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<MatchResultDto>> Create([FromBody] MatchResultDto dto)
        {
            var created = await matchResultService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] MatchResultDto dto)
        {
            await matchResultService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await matchResultService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/viewed")]
        [Authorize(Roles = "Admin,Student,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsViewed(Guid id)
        {
            await matchResultService.MarkAsViewedAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/applied")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsApplied(Guid id)
        {
            await matchResultService.MarkAsAppliedAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/hired")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsHired(Guid id, [FromBody] DateTime hireDate)
        {
            await matchResultService.MarkAsHiredAsync(id, hireDate);
            return NoContent();
        }
    }
}