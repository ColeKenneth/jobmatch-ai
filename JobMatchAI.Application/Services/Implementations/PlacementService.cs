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
    public class PlacementService(AppDbContext context, ILogger<PlacementService> logger) : IPlacementService
    {
        public async Task<PlacementRecordDto> CreateAsync(PlacementRecordDto dto)
        {
            var studentExists = await context.Students.AnyAsync(s => s.Id == dto.StudentId);
            if (!studentExists) throw new NotFoundException($"Student {dto.StudentId} not found.");

            var jobExists = await context.JobPostings.AnyAsync(jp => jp.Id == dto.JobPostingId);
            if (!jobExists) throw new NotFoundException($"Job posting {dto.JobPostingId} not found.");

            var alreadyExists = await context.PlacementRecords
                .AnyAsync(pr => pr.StudentId == dto.StudentId && pr.JobPostingId == dto.JobPostingId);

            if (alreadyExists) throw new AlreadyExistsException("A placement record already exists for this student and job posting.");

            var placementRecord = new PlacementRecord
            {
                StudentId = dto.StudentId,
                JobPostingId = dto.JobPostingId,
                PlacementDate = dto.PlacementDate,
                PlacementStatus = dto.PlacementStatus,
                Notes = dto.Notes?.Trim()
            };

            context.PlacementRecords.Add(placementRecord);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating placement record for student {StudentId} and job posting {JobPostingId}", dto.StudentId, dto.JobPostingId);
                throw new BusinessRuleException("Failed to create placement record.");
            }

            logger.LogInformation("Created placement record {PlacementRecordId} for student {StudentId} and job posting {JobPostingId}",
                placementRecord.Id, placementRecord.StudentId, placementRecord.JobPostingId);
            return MapToDto(placementRecord);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.PlacementRecords
                .Where(pr => pr.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Placement record {id} not found.");
            logger.LogInformation("Deleted placement record {PlacementRecordId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.PlacementRecords.AnyAsync(pr => pr.Id == id);
        }

        public async Task<PlacementRecordDto?> GetByIdAsync(Guid id)
        {
            return await context.PlacementRecords
                .AsNoTracking()
                .Where(pr => pr.Id == id)
                .Select(AsPlacementRecordDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PlacementRecordDto>> GetByJobPostingAsync(Guid jobPostingId)
        {
            return await context.PlacementRecords
                .AsNoTracking()
                .Where(pr => pr.JobPostingId == jobPostingId)
                .Select(AsPlacementRecordDto)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlacementRecordDto>> GetByStatusAsync(PlacementStatus status)
        {
            return await context.PlacementRecords
                .AsNoTracking()
                .Where(pr => pr.PlacementStatus == status)
                .OrderByDescending(pr => pr.PlacementDate)
                .Select(AsPlacementRecordDto)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlacementRecordDto>> GetByStudentAsync(Guid studentId)
        {
            return await context.PlacementRecords
                .AsNoTracking()
                .Where(pr => pr.StudentId == studentId)
                .Select(AsPlacementRecordDto)
                .ToListAsync();
        }

        public async Task UpdateAsync(Guid id, PlacementRecordDto dto)
        {
            var placementRecord = await context.PlacementRecords.FindAsync(id)
                ?? throw new NotFoundException($"Placement record {id} not found.");

            if (dto.StudentId != Guid.Empty && dto.StudentId != placementRecord.StudentId)
            {
                throw new BusinessRuleException("Cannot reassign a placement record to a different student.");
            }

            if (dto.JobPostingId != Guid.Empty && dto.JobPostingId != placementRecord.JobPostingId)
            {
                throw new BusinessRuleException("Cannot reassign a placement record to a different job posting.");
            }

            placementRecord.PlacementDate = dto.PlacementDate;
            placementRecord.PlacementStatus = dto.PlacementStatus;
            placementRecord.Notes = dto.Notes?.Trim();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating placement record {PlacementRecordId}", id);
                throw new BusinessRuleException("Failed to update placement record.");
            }

            logger.LogInformation("Updated placement record {PlacementRecordId}", id);
        }

        public async Task UpdateStatusAsync(Guid id, PlacementStatus status)
        {
            var updatedCount = await context.PlacementRecords
                .Where(pr => pr.Id == id)
                .ExecuteUpdateAsync(s => s
                .SetProperty(pr => pr.PlacementStatus, status));

            if (updatedCount == 0) throw new NotFoundException($"Placement record {id} not found.");

            logger.LogInformation("Updated placement status for placement record {PlacementRecordId}", id);
        }

        private static readonly Expression<Func<PlacementRecord, PlacementRecordDto>> AsPlacementRecordDto = pr => new PlacementRecordDto
        {
            Id = pr.Id,
            StudentId = pr.StudentId,
            JobPostingId = pr.JobPostingId,
            PlacementDate = pr.PlacementDate,
            PlacementStatus = pr.PlacementStatus,
            Notes = pr.Notes
        };

        private static readonly Func<PlacementRecord, PlacementRecordDto> MapToDto = AsPlacementRecordDto.Compile();
    }
}
