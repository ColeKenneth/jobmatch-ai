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
    public class JobPostingService(AppDbContext context, ILogger<JobPostingService> logger) : IJobPostingService
    {
        public async Task CloseAsync(Guid id)
        {
            var updatedCount = await context.JobPostings
                .Where(jp => jp.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(jp => jp.IsActive, false));

            if (updatedCount == 0) throw new NotFoundException($"Job posting {id} not found.");

            logger.LogInformation("Closed job posting {JobPostingId}", id);
        }

        public async Task<JobPostingDto> CreateAsync(JobPostingDto dto)
        {
            var employer = await context.Employers.FindAsync(dto.EmployerId)
                ?? throw new NotFoundException($"Employer not found: {dto.EmployerId}");

            if (dto.MinSalary.HasValue && dto.MaxSalary.HasValue && dto.MinSalary > dto.MaxSalary)
            {
                throw new BusinessRuleException("Minimum salary cannot be greater than the maximum salary.");
            }

            if (dto.Deadline.HasValue && dto.Deadline.Value <= DateTime.UtcNow)
            {
                throw new BusinessRuleException("Deadline must be in the future.");
            }

            var jobPosting = new JobPosting
            {
                EmployerId = dto.EmployerId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Requirements = dto.Requirements.Trim(),
                PreferredSkills = dto.PreferredSkills?.Trim(),
                MinExperienceYears = dto.MinExperienceYears,
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary,
                Location = dto.Location.Trim(),
                EmploymentType = dto.EmploymentType,
                Deadline = dto.Deadline,
                IsActive = true,
                Views = 0,
                Applications = 0
            };

            context.JobPostings.Add(jobPosting);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating job posting for employer {EmployerId}", dto.EmployerId);
                throw new BusinessRuleException("Failed to create job posting.");
            }

            logger.LogInformation("Created job posting {JobPostingId} for employer {EmployerId}", jobPosting.Id, jobPosting.EmployerId);
            return MapToDto(jobPosting);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.JobPostings
                .Where(jp => jp.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Job posting {id} not found.");

            logger.LogInformation("Deleted job posting {JobPostingId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.JobPostings.AnyAsync(jp => jp.Id == id);
        }

        public async Task<IEnumerable<JobPostingDto>> GetActiveJobsAsync()
        {
            return await context.JobPostings
                .AsNoTracking()
                .Where(jp => jp.IsActive && (jp.Deadline == null || jp.Deadline > DateTime.UtcNow))
                .OrderByDescending(jp => jp.PostedDate)
                .Select(AsJobPostingDto)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobPostingDto>> GetAllAsync()
        {
            return await context.JobPostings
                .AsNoTracking()
                .OrderByDescending(jp => jp.PostedDate)
                .Select(AsJobPostingDto)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobPostingDto>> GetByEmployerAsync(Guid employerId)
        {
            return await context.JobPostings
                .AsNoTracking()
                .Where(jp => jp.EmployerId == employerId)
                .OrderByDescending(jp => jp.PostedDate)
                .Select(AsJobPostingDto)
                .ToListAsync();
        }

        public async Task<JobPostingDto?> GetByIdAsync(Guid id)
        {
            return await context.JobPostings
                .AsNoTracking()
                .Where(jp => jp.Id == id)
                .Select(AsJobPostingDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<JobPostingDto>> GetByLocationAsync(string location)
        {
            var normalizedLocation = location.Trim().ToLowerInvariant();

            return await context.JobPostings
                .AsNoTracking()
                .Where(jp => jp.Location.ToLower() == normalizedLocation)
                .OrderByDescending(jp => jp.PostedDate)
                .Select(AsJobPostingDto)
                .ToListAsync();
        }

        public async Task IncrementApplicationsAsync(Guid id)
        {
            var updatedCount = await context.JobPostings
                .Where(jp => jp.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(jp => jp.Applications, jp => jp.Applications + 1));

            if (updatedCount == 0) throw new NotFoundException($"Job posting {id} not found.");
        }

        public async Task IncrementViewsAsync(Guid id)
        {
            var updatedCount = await context.JobPostings
                .Where(jp => jp.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(jp => jp.Views, jp => jp.Views + 1));

            if (updatedCount == 0) throw new NotFoundException($"Job posting {id} not found.");
        }

        public async Task ReopenAsync(Guid id)
        {
            var updatedCount = await context.JobPostings
                .Where(jp => jp.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(jp => jp.IsActive, true));

            if (updatedCount == 0) throw new NotFoundException($"Job posting {id} not found.");

            logger.LogInformation("Reopened job posting {JobPostingId}", id);
        }

        public async Task UpdateAsync(Guid id, JobPostingDto dto)
        {
            if (dto.MinSalary.HasValue && dto.MaxSalary.HasValue && dto.MinSalary > dto.MaxSalary)
            {
                throw new BusinessRuleException("Minimum salary cannot be greater than the maximum salary.");
            }

            if (dto.Deadline.HasValue && dto.Deadline.Value <= DateTime.UtcNow)
            {
                throw new BusinessRuleException("Deadline must be in the future.");
            }

            var jobPosting = await context.JobPostings.FindAsync(id)
                ?? throw new NotFoundException($"Job posting {id} not found.");

            jobPosting.Title = dto.Title.Trim();
            jobPosting.Description = dto.Description.Trim();
            jobPosting.Requirements = dto.Requirements.Trim();
            jobPosting.PreferredSkills = dto.PreferredSkills?.Trim();
            jobPosting.MinExperienceYears = dto.MinExperienceYears;
            jobPosting.MinSalary = dto.MinSalary;
            jobPosting.MaxSalary = dto.MaxSalary;
            jobPosting.Location = dto.Location.Trim();
            jobPosting.EmploymentType = dto.EmploymentType;
            jobPosting.Deadline = dto.Deadline;
            jobPosting.IsActive = dto.IsActive;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating job posting {JobPostingId}", id);
                throw new BusinessRuleException("Failed to update job posting.");
            }

            logger.LogInformation("Updated job posting {JobPostingId}", id);
        }

        private static readonly Expression<Func<JobPosting, JobPostingDto>> AsJobPostingDto = jp => new JobPostingDto
        {
            Id = jp.Id,
            EmployerId = jp.EmployerId,
            Title = jp.Title,
            Description = jp.Description,
            Requirements = jp.Requirements,
            PreferredSkills = jp.PreferredSkills,
            MinExperienceYears = jp.MinExperienceYears,
            MinSalary = jp.MinSalary,
            MaxSalary = jp.MaxSalary,
            Location = jp.Location,
            EmploymentType = jp.EmploymentType,
            PostedDate = jp.PostedDate,
            Deadline = jp.Deadline,
            IsActive = jp.IsActive,
            Views = jp.Views,
            Applications = jp.Applications
        };

        private static readonly Func<JobPosting, JobPostingDto> MapToDto = AsJobPostingDto.Compile();
    }
}
