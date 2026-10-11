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
    public class MatchResultService(AppDbContext context, ILogger<MatchResultService> logger) : IMatchResultService
    {
        public async Task<MatchResultDto> CreateAsync(MatchResultDto dto)
        {
            var studentExists = await context.Students.AnyAsync(s => s.Id == dto.StudentId);
            if (!studentExists) throw new NotFoundException($"Student {dto.StudentId} not found.");

            var jobExists = await context.JobPostings.AnyAsync(jp => jp.Id == dto.JobPostingId);
            if (!jobExists) throw new NotFoundException($"Job posting {dto.JobPostingId} not found.");

            var alreadyExists = await context.MatchResults
                .AnyAsync(mr => mr.StudentId == dto.StudentId && mr.JobPostingId == dto.JobPostingId);

            if (alreadyExists) throw new AlreadyExistsException("A match result already exists for this student and job posting.");

            var matchResult = new MatchResult
            {
                StudentId = dto.StudentId,
                JobPostingId = dto.JobPostingId,
                MatchScore = dto.MatchScore,
                EmployabilityScore = dto.EmployabilityScore,
                MatchExplanation = dto.MatchExplanation?.Trim(),
                FeatureContributions = dto.FeatureContributions?.Trim(),
                GeneratedAt = DateTime.UtcNow,
                WasViewed = false,
                WasApplied = false,
                WasHired = false
            };

            context.MatchResults.Add(matchResult);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating match result for student {StudentId} and job posting {JobPostingId}", dto.StudentId, dto.JobPostingId);
                throw new BusinessRuleException("Failed to create match result.");
            }

            logger.LogInformation("Created match result {MatchResultId} for student {StudentId} and job posting {JobPostingId}",
                matchResult.Id, matchResult.StudentId, matchResult.JobPostingId);
            return MapToDto(matchResult);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.MatchResults
                .Where(mr => mr.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Match result {id} not found.");
            logger.LogInformation("Deleted match result {MatchResultId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.MatchResults.AnyAsync(mr => mr.Id == id);
        }

        public async Task<MatchResultDto?> GetByIdAsync(Guid id)
        {
            return await context.MatchResults
                .AsNoTracking()
                .Where(mr => mr.Id == id)
                .Select(AsMatchResultDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<MatchResultDto>> GetByJobPostingAsync(Guid jobPostingId)
        {
            return await context.MatchResults
                .AsNoTracking()
                .Where(mr => mr.JobPostingId == jobPostingId)
                .OrderByDescending(mr => mr.MatchScore)
                .Select(AsMatchResultDto)
                .ToListAsync();
        }

        public async Task<MatchResultDto?> GetByStudentAndJobAsync(Guid studentId, Guid jobPostingId)
        {
            return await context.MatchResults
                .AsNoTracking()
                .Where(mr => mr.StudentId == studentId && mr.JobPostingId == jobPostingId)
                .Select(AsMatchResultDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<MatchResultDto>> GetByStudentAsync(Guid studentId)
        {
            return await context.MatchResults
                .AsNoTracking()
                .Where(mr => mr.StudentId == studentId)
                .OrderByDescending(mr => mr.MatchScore)
                .Select(AsMatchResultDto)
                .ToListAsync();
        }

        public async Task MarkAsAppliedAsync(Guid id)
        {
            var updatedCount = await context.MatchResults
                .Where(mr => mr.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(mr => mr.WasViewed, true)
                .SetProperty(mr => mr.WasApplied, true));

            if (updatedCount == 0) throw new NotFoundException($"Match result {id} not found.");

            logger.LogInformation("Marked match result {MatchResultId} as applied", id);
        }

        public async Task MarkAsHiredAsync(Guid id, DateTime hireDate)
        {
            var updatedCount = await context.MatchResults
                .Where(mr => mr.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(mr => mr.WasViewed, true)
                .SetProperty(mr => mr.WasApplied, true)
                .SetProperty(mr => mr.WasHired, true)
                .SetProperty(mr => mr.HireDate, hireDate));

            if (updatedCount == 0) throw new NotFoundException($"Match result {id} not found.");

            logger.LogInformation("Marked match result {MatchResultId} as hired on {HireDate}", id, hireDate);
        }

        public async Task MarkAsViewedAsync(Guid id)
        {
            var updatedCount = await context.MatchResults
                .Where(mr => mr.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(mr => mr.WasViewed, true));

            if (updatedCount == 0) throw new NotFoundException($"Match result {id} not found.");

            logger.LogInformation("Marked match result {MatchResultId} as viewed", id);
        }

        public async Task UpdateAsync(Guid id, MatchResultDto dto)
        {
            var matchResult = await context.MatchResults.FindAsync(id)
                ?? throw new NotFoundException($"Match result {id} not found.");

            if (dto.StudentId != Guid.Empty && dto.StudentId != matchResult.StudentId)
            {
                throw new BusinessRuleException("Cannot reassign match result to a different student.");
            }

            if (dto.JobPostingId != Guid.Empty && dto.JobPostingId != matchResult.JobPostingId)
            {
                throw new BusinessRuleException("Cannot reassign match result to a different job posting.");
            }

            matchResult.MatchScore = dto.MatchScore;
            matchResult.EmployabilityScore = dto.EmployabilityScore;
            matchResult.MatchExplanation = dto.MatchExplanation?.Trim();
            matchResult.FeatureContributions = dto.FeatureContributions?.Trim();
            matchResult.WasViewed = dto.WasViewed;
            matchResult.WasApplied = dto.WasApplied;
            matchResult.WasHired = dto.WasHired;
            matchResult.HireDate = dto.HireDate;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating match result {MatchResultId}", id);
                throw new BusinessRuleException("Failed to update match result.");
            }

            logger.LogInformation("Updated match result {MatchResultId}", id);
        }

        private static readonly Expression<Func<MatchResult, MatchResultDto>> AsMatchResultDto = mr => new MatchResultDto
        {
            Id = mr.Id,
            StudentId = mr.StudentId,
            JobPostingId = mr.JobPostingId,
            MatchScore = mr.MatchScore,
            EmployabilityScore = mr.EmployabilityScore,
            MatchExplanation = mr.MatchExplanation,
            FeatureContributions = mr.FeatureContributions,
            GeneratedAt = mr.GeneratedAt,
            WasViewed = mr.WasViewed,
            WasApplied = mr.WasApplied,
            WasHired = mr.WasHired,
            HireDate = mr.HireDate
        };

        private static readonly Func<MatchResult, MatchResultDto> MapToDto = AsMatchResultDto.Compile();
    }
}