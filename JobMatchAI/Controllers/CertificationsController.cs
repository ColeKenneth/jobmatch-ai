using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatchAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CertificationsController(ICertificationService certificationService) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CertificationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CertificationDto>> GetById(Guid id)
        {
            var cert = await certificationService.GetByIdAsync(id);
            return cert is null ? NotFound() : Ok(cert);
        }

        [HttpGet("student/{studentId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CertificationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CertificationDto>>> GetByStudent(Guid studentId)
        {
            var certs = await certificationService.GetByStudentAsync(studentId);
            return Ok(certs);
        }

        [HttpPost("student/{studentId:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(typeof(CertificationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CertificationDto>> Create(Guid studentId, [FromBody] CertificationDto dto)
        {
            var created = await certificationService.CreateAsync(studentId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CertificationDto dto)
        {
            await certificationService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await certificationService.DeleteAsync(id);
            return NoContent();
        }
    }
}