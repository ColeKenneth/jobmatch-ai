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
    public class InternshipService(AppDbContext context, ILogger<InternshipService> logger) : IInternshipService
    {
        public async Task<InternshipDto> CreateAsync(Guid studentId, InternshipDto dto)
        {
            var studentExists = await context.Students.AnyAsync(s => s.Id == studentId);

            if (!studentExists) throw new NotFoundException($"Student {studentId} not found.");

            if (dto.EndDate < dto.StartDate)
            {
                throw new BusinessRuleException("End date must be on or after the start date.");
            }

            var internship = new Internship
            {
                StudentId = studentId,
                CompanyName = dto.CompanyName.Trim(),
                Position = dto.Position.Trim(),
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                SkillsGained = dto.SkillsGained?.Trim(),
                Supervisor = dto.Supervisor?.Trim(),
                SupervisorEmail = dto.SupervisorEmail?.Trim().ToLowerInvariant()
            };

            context.Internships.Add(internship);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating internship to student {StudentId}", studentId);
                throw new BusinessRuleException("Failed to create internship.");
            }

            logger.LogInformation("Attached internship {InternshipId} to student {StudentId}", internship.Id, studentId);
            return MapToDto(internship);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.Internships
                .Where(i => i.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Internship {id} not found.");
            logger.LogInformation("Removed internship {InternshipId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Internships.AnyAsync(i => i.Id == id);
        }

        public async Task<InternshipDto?> GetByIdAsync(Guid id)
        {
            return await context.Internships
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(AsInternshipDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<InternshipDto>> GetByStudentAsync(Guid studentId)
        {
            return await context.Internships
                .AsNoTracking()
                .Where(i => i.StudentId == studentId)
                .Select(AsInternshipDto)
                .ToListAsync();
        }

        public async Task UpdateAsync(Guid id, InternshipDto dto)
        {

            if (dto.EndDate < dto.StartDate)
            {
                throw new BusinessRuleException("End date must be on or after the start date.");
            }

            var internship = await context.Internships.FindAsync(id)
                ?? throw new NotFoundException($"Internship {id} not found.");

            if (dto.StudentId != Guid.Empty && dto.StudentId != internship.StudentId)
            {
                throw new BusinessRuleException("Cannot reassign internship to a different student.");
            }

            internship.CompanyName = dto.CompanyName.Trim();
            internship.Position = dto.Position.Trim();
            internship.StartDate = dto.StartDate;
            internship.EndDate = dto.EndDate;
            internship.SkillsGained = dto.SkillsGained?.Trim();
            internship.Supervisor = dto.Supervisor?.Trim();
            internship.SupervisorEmail = dto.SupervisorEmail?.Trim().ToLowerInvariant();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating internship {InternshipId}", id);
                throw new BusinessRuleException("Failed to update internship.");
            }

            logger.LogInformation("Updated internship {InternshipId}", id);
        }

        private static readonly Expression<Func<Internship, InternshipDto>> AsInternshipDto = i => new InternshipDto
        {
            Id = i.Id,
            StudentId = i.StudentId,
            CompanyName = i.CompanyName,
            Position = i.Position,
            StartDate = i.StartDate,
            EndDate = i.EndDate,
            SkillsGained = i.SkillsGained,
            Supervisor = i.Supervisor,
            SupervisorEmail = i.SupervisorEmail
        };

        private static readonly Func<Internship, InternshipDto> MapToDto = AsInternshipDto.Compile();
    }
}
