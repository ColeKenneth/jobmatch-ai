using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace JobMatchAI.Application.Services.Implementations
{
    public class EmployerService(AppDbContext context, ILogger<EmployerService> logger) : IEmployerService
    {
        public async Task<EmployerDto> CreateAsync(EmployerDto dto)
        {
            var user = await context.Users.FindAsync(dto.UserId)
                ?? throw new NotFoundException($"User not found: {dto.UserId}");

            if (user.Role != UserRole.Employer)
            {
                throw new BusinessRuleException("User must have the employer role.");
            }

            var profileExists = await context.Employers.AnyAsync(e => e.UserId == dto.UserId);

            if (profileExists) throw new AlreadyExistsException($"An employer profile already exists for user {dto.UserId}");

            var trimmedCompanyName = dto.CompanyName.Trim();
            var companyExists = await context.Employers
                .AnyAsync(e => e.CompanyName.ToLower() == trimmedCompanyName.ToLower());

            if (companyExists) throw new AlreadyExistsException($"Employer with company name '{dto.CompanyName}' already exists.");

            var employer = new Employer
            {
                UserId = dto.UserId,
                CompanyName = trimmedCompanyName,
                Industry = dto.Industry.Trim(),
                Website = dto.Website?.Trim(),
                CompanyDescription = dto.CompanyDescription?.Trim(),
                LogoUrl = dto.LogoUrl?.Trim()
            };

            context.Employers.Add(employer);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating employer for user {UserId}", dto.UserId);
                throw new AlreadyExistsException($"Employer '{dto.CompanyName}' already exists.");
            }

            logger.LogInformation("Created employer {Id}", employer.Id);
            return MapToDto(employer);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.Employers
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Employer {id} not found.");

            logger.LogInformation("Deleted employer with an ID of {Id}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Employers.AnyAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<EmployerDto>> GetAllAsync()
        {
            return await context.Employers
                .AsNoTracking()
                .Select(AsEmployerDto)
                .ToListAsync();
        }

        public async Task<EmployerDto?> GetByIdAsync(Guid id)
        {
            return await context.Employers
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(AsEmployerDto)
                .FirstOrDefaultAsync();
        }

        public async Task<EmployerDto?> GetByUserIdAsync(Guid userId)
        {
            return await context.Employers
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(AsEmployerDto)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Guid id, EmployerDto dto)
        {
            var trimmedCompanyName = dto.CompanyName.Trim();
            var companyExists = await context.Employers
                .AnyAsync(e => e.Id != id && e.CompanyName.ToLower() == trimmedCompanyName.ToLower());

            if (companyExists) throw new AlreadyExistsException($"Employer with company name '{dto.CompanyName}' already exists.");

            var employer = await context.Employers.FindAsync(id)
                ?? throw new NotFoundException($"Employer {id} not found.");

            employer.CompanyName = trimmedCompanyName;
            employer.Industry = dto.Industry.Trim();
            employer.Website = dto.Website?.Trim();
            employer.CompanyDescription = dto.CompanyDescription?.Trim();
            employer.LogoUrl = dto.LogoUrl?.Trim();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating employer {Id}", id);
                throw new AlreadyExistsException($"Employer {id} already exists.");
            }

            logger.LogInformation("Updated employer {EmployerId}", employer.Id);
        }

        private static readonly Expression<Func<Employer, EmployerDto>> AsEmployerDto = e => new EmployerDto
        {
            Id = e.Id,
            UserId = e.UserId,
            CompanyName = e.CompanyName,
            Industry = e.Industry,
            Website = e.Website,
            CompanyDescription = e.CompanyDescription,
            LogoUrl = e.LogoUrl
        };

        private static readonly Func<Employer, EmployerDto> MapToDto = AsEmployerDto.Compile();
    }
}