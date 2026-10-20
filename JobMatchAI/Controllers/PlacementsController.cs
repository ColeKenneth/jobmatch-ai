using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlacementsController(IPlacementService placementService) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PlacementRecordDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PlacementRecordDto>> GetById(Guid id)
        {
            var placement = await placementService.GetByIdAsync(id);
            return placement is null ? NotFound() : Ok(placement);
        }

        [HttpGet("student/{studentId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<PlacementRecordDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PlacementRecordDto>>> GetByStudent(Guid studentId)
        {
            var placements = await placementService.GetByStudentAsync(studentId);
            return Ok(placements);
        }

        [HttpGet("job/{jobPostingId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<PlacementRecordDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PlacementRecordDto>>> GetByJobPosting(Guid jobPostingId)
        {
            var placements = await placementService.GetByJobPostingAsync(jobPostingId);
            return Ok(placements);
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Employer,PlacementOfficer")]
        [ProducesResponseType(typeof(IEnumerable<PlacementRecordDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PlacementRecordDto>>> GetByStatus(PlacementStatus status)
        {
            var placements = await placementService.GetByStatusAsync(status);
            return Ok(placements);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employer,PlacementOfficer")]
        [ProducesResponseType(typeof(PlacementRecordDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<PlacementRecordDto>> Create([FromBody] PlacementRecordDto dto)
        {
            var created = await placementService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Employer,PlacementOfficer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] PlacementRecordDto dto)
        {
            await placementService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin,Employer,PlacementOfficer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] PlacementStatus status)
        {
            await placementService.UpdateStatusAsync(id, status);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await placementService.DeleteAsync(id);
            return NoContent();
        }
    }
}