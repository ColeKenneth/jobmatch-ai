using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController(IMatchingEngineService matchingEngineService) : ControllerBase
{
    [HttpGet("evaluate")]
    [Authorize(Roles = "Admin,Employer,Student")]
    [ProducesResponseType(typeof(XaiExplanation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<XaiExplanation>> EvaluateMatch(
        [FromQuery] Guid studentId,
        [FromQuery] Guid jobPostingId,
        CancellationToken ct)
    {
        var explanation = await matchingEngineService.EvaluateMatchAsync(studentId, jobPostingId, ct);
        return Ok(explanation);
    }
}