using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController(IResumeParserService resumeParserService, ISkillExtractorService skillExtractorService) : ControllerBase
{
    [HttpPost("extract-skills")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ExtractSkills(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Please upload a valid PDF or DOCX file.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var resumeText = await resumeParserService.ExtractTextAsync(stream, file.FileName);

            if (string.IsNullOrWhiteSpace(resumeText))
            {
                return BadRequest("Could not extract text from the provided document.");
            }

            var extractedData = await skillExtractorService.ExtractSkillsAndMetadataAsync(resumeText);

            var response = new ResumeExtractionResponse(
                FileName: file.FileName,
                ExtractedSkills: [.. extractedData.ExtractedSkills.Select(s => s.SkillName)],
                TotalSkillsFound: extractedData.ExtractedSkills.Count
            );

            return Ok(response);
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while processing the resume: {ex.Message}");
        }
    }
}