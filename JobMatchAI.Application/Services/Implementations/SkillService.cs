using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace JobMatchAI.Application.Services.Implementations
{
    public class SkillService(AppDbContext context, ILogger<SkillService> logger) : ISkillService
    {
        public async Task<SkillDto> CreateAsync(SkillDto dto)
        {
            var trimmedName = dto.Name.Trim();
            var exists = await context.Skills
                .AnyAsync(s => s.Name.ToLower() == trimmedName.ToLower());

            if (exists) throw new AlreadyExistsException($"Skill {dto.Name} already exists.");

            var skill = new Skill
            {
                Name = trimmedName,
                Category = dto.Category,
                OntologyId = dto.OntologyId?.Trim(),
                Description = dto.Description?.Trim(),
                IconUrl = dto.IconUrl?.Trim()
            };

            context.Skills.Add(skill);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating skill {Name}", dto.Name);
                throw new AlreadyExistsException($"Skill {dto.Name} already exists.");
            }

            logger.LogInformation("Created skill {SkillId} ({Name})", skill.Id, skill.Name);
            return MapToDto(skill);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.Skills
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Skill {id} not found.");

            logger.LogInformation("Deleted skill with an ID of {SkillId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Skills.AnyAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<SkillDto>> GetAllAsync()
        {
            return await context.Skills
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(AsSkillDto)
                .ToListAsync();
        }

        public async Task<IEnumerable<SkillDto>> GetByCategoryAsync(Category category)
        {
            return await context.Skills
                .AsNoTracking()
                .Where(s => s.Category == category)
                .OrderBy(s => s.Name)
                .Select(AsSkillDto)
                .ToListAsync();
        }

        public async Task<SkillDto?> GetByIdAsync(Guid id)
        {
            return await context.Skills
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(AsSkillDto)
                .FirstOrDefaultAsync();
        }

        public async Task<SkillDto?> GetByNameAsync(string name)
        {
            var normalizedName = name.Trim().ToLower();

            return await context.Skills
                .AsNoTracking()
                .Where(s => s.Name.ToLower() == normalizedName)
                .Select(AsSkillDto)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Guid id, SkillDto dto)
        {
            var trimmedName = dto.Name.Trim();
            var exists = await context.Skills
                .AnyAsync(s => s.Id != id && s.Name.ToLower() == trimmedName.ToLower());

            if (exists) throw new AlreadyExistsException($"Skill {dto.Name} already exists.");

            var skill = await context.Skills.FindAsync(id)
                ?? throw new NotFoundException($"Skill {id} not found.");

            skill.Name = trimmedName;
            skill.Category = dto.Category;
            skill.OntologyId = dto.OntologyId?.Trim();
            skill.Description = dto.Description?.Trim();
            skill.IconUrl = dto.IconUrl?.Trim();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating skill {SkillId}", id);
                throw new AlreadyExistsException($"Skill {dto.Name} already exists.");
            }

            logger.LogInformation("Updated skill {SkillId}", skill.Id);
        }

        private static readonly Expression<Func<Skill, SkillDto>> AsSkillDto = s => new SkillDto
        {
            Id = s.Id,
            Name = s.Name,
            Category = s.Category,
            OntologyId = s.OntologyId,
            Description = s.Description,
            IconUrl = s.IconUrl
        };

        private static readonly Func<Skill, SkillDto> MapToDto = AsSkillDto.Compile();
    }
}