using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobMatchAI.Application.Services.Implementations
{
    public class SkillService(AppDbContext context, ILogger<SkillService> logger) : ISkillService
    {
        public async Task<SkillDto> CreateAsync(SkillDto dto)
        {
            var exists = await context.Skills
                .AnyAsync(s => EF.Functions.ILike(s.Name, dto.Name));

            if (exists) throw new InvalidOperationException($"Skill {dto.Name} already exists.");

            var skill = new Skill
            {
                Name = dto.Name,
                Category = dto.Category,
                OntologyId = dto.OntologyId,
                Description = dto.Description,
                IconUrl = dto.IconUrl
            };

            try
            {
                await context.SaveChangesAsync();
            } 
            catch (DbUpdateException)
            {
                throw new InvalidOperationException($"Skill {dto.Name} already exists.");
            }

            logger.LogInformation("Created skill {SkillId} ({Name})", skill.Id, skill.Name);

            return MapToDto(skill);
        }

        public async Task DeleteAsync(Guid id)
        {
            var skill = await context.Skills.FindAsync(id)
                ?? throw new KeyNotFoundException($"Skill {id} not found.");

            context.Skills.Remove(skill);
            await context.SaveChangesAsync();
            logger.LogInformation("Deleted skill with an ID of {SkillId}", skill.Id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Skills.AnyAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<SkillDto>> GetAllAsync()
        {
            var skills = await context.Skills
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();

            return skills.Select(MapToDto);
        }

        public async Task<IEnumerable<SkillDto>> GetByCategoryAsync(Category category)
        {
            var skills = await context.Skills
                .AsNoTracking()
                .Where(s => s.Category == category)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return skills.Select(MapToDto);
        }

        public async Task<SkillDto?> GetByIdAsync(Guid id)
        {
            var skill = await context.Skills
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            return skill is null ? null : MapToDto(skill);
        }

        public async Task<SkillDto?> GetByNameAsync(string name)
        {
            var skill = await context.Skills
                .AsNoTracking()
                .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, name));

            return skill is null ? null : MapToDto(skill);
        }

        public async Task UpdateAsync(Guid id, SkillDto dto)
        {
            var exists = await context.Skills
                .AnyAsync(s => s.Id != id && EF.Functions.ILike(s.Name, dto.Name));

            if (exists) throw new InvalidOperationException($"Skill {dto.Name} already exists.");

            var skill = await context.Skills.FindAsync(id)
                ?? throw new KeyNotFoundException($"Skill {id} not found.");

            skill.Name = dto.Name;
            skill.Category = dto.Category;
            skill.OntologyId = dto.OntologyId;
            skill.Description = dto.Description;
            skill.IconUrl = dto.IconUrl;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException($"Skill {dto.Name} already exists.");
            }
            logger.LogInformation("Updated skill {SkillId}", skill.Id);
        }

        private static SkillDto MapToDto(Skill skill) => new()
        {
            Id = skill.Id,
            Name = skill.Name,
            Category = skill.Category,
            OntologyId = skill.OntologyId,
            Description = skill.Description,
            IconUrl = skill.IconUrl
        };
    }
}
