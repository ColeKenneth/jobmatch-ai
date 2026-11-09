using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController(IStudentService studentService) : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<StudentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
        {
            var students = await studentService.GetAllAsync();
            return Ok(students);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentDto>> GetById(Guid id)
        {
            var student = await studentService.GetByIdAsync(id);
            return student is null ? NotFound() : Ok(student);
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentDto>> GetByUserId(Guid userId)
        {
            var student = await studentService.GetByUserIdAsync(userId);
            return student is null ? NotFound() : Ok(student);
        }

        [HttpGet("number/{studentIdNumber}")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentDto>> GetByStudentNumber(string studentIdNumber)
        {
            var student = await studentService.GetByStudentIdNumberAsync(studentIdNumber);
            return student is null ? NotFound() : Ok(student);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<StudentDto>> Create([FromBody] StudentDto dto)
        {
            var created = await studentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] StudentDto dto)
        {
            await studentService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpPost("{id:guid}/upload-resume")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadResume(Guid id, IFormFile? file, [FromServices] IResumeParserService parserService)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            const long maxFileSize = 30 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest("File size exceeds the 30MB limit.");
            }

            var allowedExtensions = new[] { ".pdf", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file type uploaded. Only PDF and DOCX are allowed.");
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var extractedText = await parserService.ExtractTextAsync(stream, file.FileName);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return BadRequest("Could not extract text from the file. Please ensure it is not a scanned image.");
                }

                await studentService.UpdateResumeAsync(id, file.FileName, extractedText);
                return Ok(new { message = "Resume uploaded successfully.", textLength = extractedText.Length });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing the file.");
            }
            
        }

        [HttpPut("{id:guid}/resume")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResume(Guid id, [FromBody] UpdateResumeRequest request)
        {
            await studentService.UpdateResumeAsync(id, request.FilePath, request.ResumeText);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await studentService.DeleteAsync(id);
            return NoContent();
        }
    }
}
