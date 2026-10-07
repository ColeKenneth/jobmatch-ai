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
    public class UserService(AppDbContext context, ILogger<UserService> logger) : IUserService
    {
        public async Task ActivateAsync(Guid id)
        {
            var updatedCount = await context.Users
                .Where(u => u.Id == id && !u.IsActive)
                .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IsActive, true)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

            if (updatedCount == 0)
            {
                var exists = await context.Users.AnyAsync(u => u.Id == id);
                if (!exists) throw new NotFoundException($"User {id} not found.");

                return;
            }

            logger.LogInformation("Activated user {Id}", id);
        }

        public async Task<UserDto> CreateAsync(UserDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var exists = await context.Users
                .AnyAsync(u => u.Email == normalizedEmail);

            if (exists) throw new AlreadyExistsException($"User {dto.Email} already exists.");

            var user = new User
            { 
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Email = normalizedEmail,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = dto.IsActive
            };

            context.Users.Add(user);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating user {Email}", dto.Email);
                throw new AlreadyExistsException($"User {dto.Email} already exists.");
            }

            logger.LogInformation("Created user {FullName}", dto.FullName);
            return MapToDto(user);
        }

        public async Task DeactivateAsync(Guid id)
        {
            var updatedCount = await context.Users
                .Where(u => u.Id == id && u.IsActive)
                .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IsActive, false)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));

            if (updatedCount == 0)
            {
                var exists = await context.Users.AnyAsync(u => u.Id == id);
                if (!exists) throw new NotFoundException($"User {id} not found.");

                return;
            }

            logger.LogInformation("Deactivated user {Id}", id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"User {id} not found.");

            logger.LogInformation("Deleted user {Id}", id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await context.Users.AnyAsync(u => u.Email == normalizedEmail);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            return await context.Users
                 .AsNoTracking()
                 .OrderBy(u => u.LastName)
                 .Select(AsUserDto )
                 .ToListAsync();
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            return await context.Users
                .AsNoTracking()
                .Where(u => u.Email == normalizedEmail)
                .Select(AsUserDto)
                .FirstOrDefaultAsync();
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            return await context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(AsUserDto)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Guid id, UserDto dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var exists = await context.Users
                .AnyAsync(u => u.Id != id && u.Email == normalizedEmail);

            if (exists) throw new AlreadyExistsException($"User {dto.Email} already exists.");

            var user = await context.Users.FindAsync(id)
                ?? throw new NotFoundException($"User {id} not found.");

            user.FirstName = dto.FirstName;
            user.MiddleName = dto.MiddleName;
            user.LastName = dto.LastName;
            user.Email = normalizedEmail;
            user.Role = dto.Role;
            user.IsActive = dto.IsActive;

            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                logger.LogError("Error updating user {Email}", dto.Email);
                throw new AlreadyExistsException($"User {dto.Email} already exists.");
            }

            logger.LogInformation("Updated user {Email}", user.Email);
        }

        private static readonly Expression<Func<User, UserDto>> AsUserDto = u => new UserDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            MiddleName = u.MiddleName ?? "",
            LastName = u.LastName,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt,
            IsActive = u.IsActive
        };

        private static readonly Func<User, UserDto> MapToDto = AsUserDto.Compile();
    }
}
