using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobPostingsController(IJobPostingService jobPostingService) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<JobPostingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<JobPostingDto>>> GetAll()
        {
            var jobpostings = await jobPostingService.GetAllAsync();
            return Ok(jobpostings);
        }

        [HttpGet("active")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<JobPostingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]

        public async Task<ActionResult<IEnumerable<JobPostingDto>>> GetActiveJobs()
        {
            var jobpostings = await jobPostingService.GetActiveJobsAsync();
            return Ok(jobpostings);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(JobPostingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public async Task<ActionResult<JobPostingDto>> GetById(Guid id)
        {
            var jobPosting = await jobPostingService.GetByIdAsync(id);
            return jobPosting is null ? NotFound() : Ok(jobPosting);
        }

        [HttpGet("employer/{employerId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<JobPostingDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobPostingDto>>> GetByEmployer(Guid employerId)
        {
            var jobPosting = await jobPostingService.GetByEmployerAsync(employerId);
            return Ok(jobPosting);
        }

        [HttpGet("location/{location}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<JobPostingDto>), StatusCodes.Status200OK)]

        public async Task<ActionResult<IEnumerable<JobPostingDto>>> GetByLocation(string location)
        {
            var jobPosting = await jobPostingService.GetByLocationAsync(location);
            return Ok(jobPosting);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(typeof(JobPostingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<JobPostingDto>> Create([FromBody] JobPostingDto dto)
        {
            var created = await jobPostingService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] JobPostingDto dto)
        {
            await jobPostingService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await jobPostingService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/close")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Close(Guid id)
        {
            await jobPostingService.CloseAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/reopen")]
        [Authorize(Roles = "Admin,Employer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reopen(Guid id)
        {
            await jobPostingService.ReopenAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/views")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> IncrementViews(Guid id)
        {
            await jobPostingService.IncrementViewsAsync(id);
            return NoContent();
        }

        [HttpPatch("{id:guid}/applications")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> IncrementApplications(Guid id)
        {
            await jobPostingService.IncrementApplicationsAsync(id);
            return NoContent();
        }
    }
}
