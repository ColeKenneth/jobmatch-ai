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
    public class SkillsController(ISkillService skillService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SkillDto>), StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetAll()
        {
            var skills = await skillService.GetAllAsync();
            return Ok(skills);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        public async Task<ActionResult<SkillDto>> GetById(Guid id)
        {
            var skill = await skillService.GetByIdAsync(id);
            return skill is null ? NotFound() : Ok(skill);
        }

        [HttpGet("name/{name}")]
        [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        public async Task<ActionResult<SkillDto>> GetByName(string name)
        {
            var skill = await skillService.GetByNameAsync(name);
            return skill is null ? NotFound() : Ok(skill);
        }

        [HttpGet("category/{category}")]
        [ProducesResponseType(typeof(IEnumerable<SkillDto>), StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SkillDto>>> GetByCategory(Category category)
        {
            var skills = await skillService.GetByCategoryAsync(category);
            return Ok(skills);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SkillDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SkillDto>> Create([FromBody] SkillDto dto)
        {
            var created = await skillService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SkillDto dto)
        {
            await skillService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await skillService.DeleteAsync(id);
            return NoContent();
        }
    }
}
