using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace JobMatchAI.Application.Services.Implementations
{
    public class JobSkillService(AppDbContext context, ILogger<JobSkillService> logger) : IJobSkillService
    {
        public async Task<JobSkillDto> AddSkillAsync(Guid jobPostingId, JobSkillDto dto)
        {
            var jobExists = await context.JobPostings.AnyAsync(jp => jp.Id == jobPostingId);

            if (!jobExists) throw new NotFoundException($"Job posting {jobPostingId} not found.");

            var skillExists = await context.Skills.AnyAsync(s => s.Id == dto.SkillId);

            if (!skillExists) throw new NotFoundException($"Skill {dto.SkillId} not found.");

            var alreadyAttached = await context.JobSkills
                .AnyAsync(js => js.JobPostingId == jobPostingId && js.SkillId == dto.SkillId);

            if (alreadyAttached) throw new AlreadyExistsException("Skill is already attached to this job posting.");

            var jobSkill = new JobSkill
            {
                JobPostingId = jobPostingId,
                SkillId = dto.SkillId,
                IsRequired = dto.IsRequired,
                MinimumProficiencyLevel = dto.MinimumProficiencyLevel,
                Priority = dto.Priority
            };

            context.JobSkills.Add(jobSkill);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating job skill for job posting {JobPostingId}", jobPostingId);
                throw new BusinessRuleException("Failed to create job skill.");
            }

            logger.LogInformation("Attached skill {SkillId} to job {JobPostingId}", dto.SkillId, jobPostingId);
            return MapToDto(jobSkill);
        }

        public async Task<bool> ExistsAsync(Guid jobPostingId, Guid skillId)
        {
            return await context.JobSkills.AnyAsync(js => js.JobPostingId == jobPostingId && js.SkillId == skillId);
        }

        public async Task<JobSkillDto?> GetByIdAsync(Guid id)
        {
            return await context.JobSkills
                .AsNoTracking()
                .Where(js => js.Id == id)
                .Select(AsJobSkillDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<JobSkillDto>> GetByJobPostingAsync(Guid jobPostingId)
        {
            return await context.JobSkills
                .AsNoTracking()
                .Where(js => js.JobPostingId == jobPostingId)
                .OrderByDescending(js => js.Priority)
                .Select(AsJobSkillDto)
                .ToListAsync();
        }

        public async Task RemoveAllForJobPostingAsync(Guid jobPostingId)
        {
            var deletedCount = await context.JobSkills
                .Where(js => js.JobPostingId == jobPostingId)
                .ExecuteDeleteAsync();

            if (deletedCount == 0)
            {
                var jobExists = await context.JobPostings.AnyAsync(jp => jp.Id == jobPostingId);
                if (!jobExists) throw new NotFoundException($"Job posting {jobPostingId} not found.");

                return;
            }

            logger.LogInformation("Removed all skills from job posting {JobPostingId}", jobPostingId);
        }

        public async Task RemoveSkillAsync(Guid id)
        {
            var deleted = await context.JobSkills
                .Where(js => js.Id == id)
                .ExecuteDeleteAsync();

            if (deleted == 0) throw new NotFoundException($"Job skill {id} not found");
            logger.LogInformation("Removed job skill {Id}", id);
        }

        public async Task UpdateSkillAsync(Guid id, JobSkillDto dto)
        {
            var jobSkill = await context.JobSkills.FindAsync(id)
                ?? throw new NotFoundException($"Job skill {id} not found.");

            jobSkill.IsRequired = dto.IsRequired;
            jobSkill.MinimumProficiencyLevel = dto.MinimumProficiencyLevel;
            jobSkill.Priority = dto.Priority;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating skill {SkillId}", id);
                throw new BusinessRuleException("Failed to update job skill.");
            }

            logger.LogInformation("Updated job skill {SkillId}", id);
        }

        private static readonly Expression<Func<JobSkill, JobSkillDto>> AsJobSkillDto = js => new JobSkillDto
        {
            Id = js.Id,
            JobPostingId = js.JobPostingId,
            SkillId = js.SkillId,
            IsRequired = js.IsRequired,
            MinimumProficiencyLevel = js.MinimumProficiencyLevel,
            Priority = js.Priority
        };

        private static readonly Func<JobSkill, JobSkillDto> MapToDto = AsJobSkillDto.Compile();
    }
}
