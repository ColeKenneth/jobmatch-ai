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
    public class EmploymentHistoryService(AppDbContext context, ILogger<EmploymentHistoryService> logger) : IEmploymentHistoryService
    {
        private static void ValidateEmploymentDates(EmploymentHistoryDto dto)
        {
            if (dto.IsCurrent && dto.EndDate.HasValue)
            {
                throw new BusinessRuleException("A current position cannot have an end date.");
            }

            if (!dto.IsCurrent && !dto.EndDate.HasValue)
            {
                throw new BusinessRuleException("A past position must have an end date.");
            }

            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            {
                throw new BusinessRuleException("End date must be on or after the start date.");
            }
        }
        public async Task<EmploymentHistoryDto> CreateAsync(Guid alumniId, EmploymentHistoryDto dto)
        {
            var alumnusExists = await context.Alumni.AnyAsync(a => a.Id == alumniId);

            if (!alumnusExists) throw new NotFoundException($"Alumni {alumniId} not found.");

            ValidateEmploymentDates(dto);

            var employmentHistory = new EmploymentHistory
            {
                AlumniId = alumniId,
                CompanyName = dto.CompanyName.Trim(),
                Position = dto.Position.Trim(),
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsCurrent = dto.IsCurrent,
                Responsibilities = dto.Responsibilities?.Trim()
            };

            context.EmploymentHistories.Add(employmentHistory);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating employment history for alumni {AlumniId}", alumniId);
                throw new BusinessRuleException("Failed to create employment history");
            }

            logger.LogInformation("Attached employment history {EmploymentHistoryId} to alumni {AlumniId}", employmentHistory.Id, alumniId);
            return MapToDto(employmentHistory);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.EmploymentHistories
                .Where(eh => eh.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Employment history {id} not found.");
            logger.LogInformation("Removed employment history {EmploymentHistoryId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.EmploymentHistories.AnyAsync(eh => eh.Id == id);
        }

        public async Task<IEnumerable<EmploymentHistoryDto>> GetByAlumniAsync(Guid alumniId)
        {
            return await context.EmploymentHistories
                .AsNoTracking()
                .Where(eh => eh.AlumniId == alumniId)
                .OrderByDescending(eh => eh.StartDate)
                .Select(AsEmploymentHistoryDto)
                .ToListAsync();
        }

        public async Task<EmploymentHistoryDto?> GetByIdAsync(Guid id)
        {
            return await context.EmploymentHistories
                .AsNoTracking()
                .Where(eh => eh.Id == id)
                .Select(AsEmploymentHistoryDto)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Guid id, EmploymentHistoryDto dto)
        {
            ValidateEmploymentDates(dto);

            var employmentHistory = await context.EmploymentHistories.FindAsync(id)
                ?? throw new NotFoundException($"Employment history {id} not found.");

            if (dto.AlumniId != Guid.Empty && dto.AlumniId != employmentHistory.AlumniId)
            {
                throw new BusinessRuleException("Cannot reassign employment history to a different alumni.");
            }

            employmentHistory.CompanyName = dto.CompanyName.Trim();
            employmentHistory.Position = dto.Position.Trim();
            employmentHistory.StartDate = dto.StartDate;
            employmentHistory.EndDate = dto.EndDate;
            employmentHistory.IsCurrent = dto.IsCurrent;
            employmentHistory.Responsibilities = dto.Responsibilities?.Trim();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating employment history {EmploymentHistoryId}", id);
                throw new BusinessRuleException("Failed to update employment history");
            }

            logger.LogInformation("Updated employment history {EmploymentHistoryId}", id);
        }

        private static readonly Expression<Func<EmploymentHistory, EmploymentHistoryDto>> AsEmploymentHistoryDto = eh => new EmploymentHistoryDto
        {
            Id = eh.Id,
            AlumniId = eh.AlumniId,
            CompanyName = eh.CompanyName,
            Position = eh.Position,
            StartDate = eh.StartDate,
            EndDate = eh.EndDate,
            IsCurrent = eh.IsCurrent,
            Responsibilities = eh.Responsibilities
        };

        private static readonly Func<EmploymentHistory, EmploymentHistoryDto> MapToDto = AsEmploymentHistoryDto.Compile();
    }
}
