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
    public class AlumniService(AppDbContext context, ILogger<AlumniService> logger) : IAlumniService
    {
        public async Task<AlumniDto> CreateAsync(AlumniDto dto)
        {
            var user = await context.Users.FindAsync(dto.UserId)
                ?? throw new NotFoundException($"User {dto.UserId} not found.");

            if (user.Role != UserRole.Alumni)
            {
                throw new BusinessRuleException("User must have the alumni role.");
            }

            var profileExists = await context.Alumni.AnyAsync(a => a.UserId == dto.UserId);

            if (profileExists) throw new AlreadyExistsException($"An alumni already exists for user {dto.UserId}");

            var alumnus = new Alumni
            {
                UserId = dto.UserId,
                Program = dto.Program.Trim(),
                GraduationDate = dto.GraduationDate,
                CurrentEmployer = dto.CurrentEmployer?.Trim(),
                CurrentPosition = dto.CurrentPosition?.Trim()
            };

            context.Alumni.Add(alumnus);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating alumni {AlumniId}", alumnus.Id);
                throw new BusinessRuleException("Failed to create alumni.");
            }

            logger.LogInformation("Created alumni {AlumniId}", alumnus.Id);
            return MapToDto(alumnus);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.Alumni
                .Where(a => a.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Alumni {id} not found.");

            logger.LogInformation("Deleted alumni profile {AlumniId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Alumni.AnyAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<AlumniDto>> GetAllAsync()
        {
            return await context.Alumni
                .AsNoTracking()
                .OrderBy(a => a.GraduationDate)
                .Select(AsAlumniDto)
                .ToListAsync();
        }

        public async Task<AlumniDto?> GetByIdAsync(Guid id)
        {
            return await context.Alumni
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(AsAlumniDto)
                .FirstOrDefaultAsync();
        }

        public async Task<AlumniDto?> GetByUserIdAsync(Guid userId)
        {
            return await context.Alumni
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(AsAlumniDto)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Guid id, AlumniDto dto)
        {
            var alumnus = await context.Alumni.FindAsync(id)
                ?? throw new NotFoundException($"Alumni {id} not found.");

            if (dto.UserId != Guid.Empty && dto.UserId != alumnus.UserId)
            {
                throw new BusinessRuleException("Cannot reassign alumni profile to a different user.");
            }

            alumnus.Program = dto.Program.Trim();
            alumnus.GraduationDate = dto.GraduationDate;
            alumnus.CurrentEmployer = dto.CurrentEmployer?.Trim();
            alumnus.CurrentPosition = dto.CurrentPosition?.Trim();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating alumni profile {AlumniId}", id);
                throw new BusinessRuleException("Failed to update alumni profile.");
            }

            logger.LogInformation("Updated alumni profile {AlumniId}", id);
        }

        private static readonly Expression<Func<Alumni, AlumniDto>> AsAlumniDto = a => new AlumniDto
        {
            Id = a.Id,
            UserId = a.UserId,
            Program = a.Program,
            GraduationDate = a.GraduationDate,
            CurrentEmployer = a.CurrentEmployer,
            CurrentPosition = a.CurrentPosition
        };

        private static readonly Func<Alumni, AlumniDto> MapToDto = AsAlumniDto.Compile();
    }
}
