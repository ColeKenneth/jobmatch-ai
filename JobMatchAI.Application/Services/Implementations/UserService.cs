using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobMatchAI.Application.Services.Implementations
{
    public class UserService(AppDbContext context, ILogger<UserService> logger) : IUserService
    {
        public async Task ActivateAsync(Guid id)
        {
            var user = await context.Users.FindAsync(id)
                ?? throw new NotFoundException($"User {id} not found.");

            if (user.IsActive) return;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            logger.LogInformation("Activated user {Id}", id);
        }

        public async Task<UserDto> CreateAsync(UserDto dto)
        {
            var exists = await context.Users
                .AnyAsync(u => u.Email == dto.Email);

            if (exists) throw new AlreadyExistsException($"User {dto.Email} already exists.");

            var user = new User
            { 
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Email = dto.Email,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = dto.IsActive
            };

            context.Users.Add(user);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new AlreadyExistsException($"User {dto.Email} already exists.");
            }

            logger.LogInformation("Created user {FullName}", dto.FullName);
            return MapToDto(user);
        }

        public async Task DeactivateAsync(Guid id)
        {
            var user = await context.Users.FindAsync(id)
                ?? throw new NotFoundException($"User {id} not found.");

            if (!user.IsActive) return;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            logger.LogInformation("Deactivated user {Id}", id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await context.Users.FindAsync(id)
                ?? throw new NotFoundException($"User {id} not found.");

            context.Users.Remove(user);
            await context.SaveChangesAsync();
            logger.LogInformation("Deleted user {Id}", user.Id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await context.Users
                .AsNoTracking()
                .OrderBy(u => u.LastName)
                .ToListAsync();

            return users.Select(MapToDto);
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            return user is null ? null : MapToDto(user);
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            return user is null ? null : MapToDto(user);
        }

        public async Task UpdateAsync(Guid id, UserDto dto)
        {
            var exists = await context.Users
                .AnyAsync(u => u.Id != id && u.Email == dto.Email);

            if (exists) throw new AlreadyExistsException($"User {dto.Email} already exists.");

            var user = await context.Users.FindAsync(id)
                ?? throw new NotFoundException($"User {id} not found.");

            user.FirstName = dto.FirstName;
            user.MiddleName = dto.MiddleName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.Role = dto.Role;
            user.IsActive = dto.IsActive;

            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new AlreadyExistsException($"User {dto.Email} already exists.");
            }

            logger.LogInformation("Updated user {Email}", user.Email);
        }

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName ?? "",
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}
