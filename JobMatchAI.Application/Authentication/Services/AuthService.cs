using JobMatchAI.Application.Authentication.DTOs;
using JobMatchAI.Application.Authentication.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobMatchAI.Application.Authentication.Services
{
    public class AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtGenerator,
        ILogger<AuthService> logger) : IAuthService
    {
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail)
                ?? throw new BusinessRuleException("Invalid email or password.");

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new BusinessRuleException("Invalid email or password.");
            }

            if (!user.IsActive) throw new BusinessRuleException("Your account has been deactivated.");

            logger.LogInformation("User {UserId} logged in.", user.Id);

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var emailExists = await context.Users
                .AnyAsync(u => u.Email == normalizedEmail);

            if (emailExists) throw new AlreadyExistsException($"Email {request.Email} is already registered.");

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                MiddleName = request.MiddleName?.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                PasswordHash = passwordHasher.Hash(request.Password),
                Role = request.Role,
                IsActive = true
            };

            context.Users.Add(user);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error registering user {Email}", normalizedEmail);
                throw new BusinessRuleException("Failed to register user.");
            }

            logger.LogInformation("Registered user {UserId} with role {Role}", user.Id, user.Role);

            return BuildAuthResponse(user);
        }

        private AuthResponse BuildAuthResponse(User user)
        {
            var (token, expiresAt) = jwtGenerator.GenerateToken(user);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = expiresAt
            };
        }
    }
}
