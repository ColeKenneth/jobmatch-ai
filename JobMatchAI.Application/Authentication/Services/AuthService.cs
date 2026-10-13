using JobMatchAI.Application.Authentication.DTOs;
using JobMatchAI.Application.Authentication.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

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

            var refreshToken = GenerateRefreshToken(user.Id);
            context.RefreshTokens.Add(refreshToken);
            
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error saving refresh token during login for user {UserId}", user.Id);
                throw new BusinessRuleException("Failed to process login.");
            }

            logger.LogInformation("User {UserId} logged in.", user.Id);
            return BuildAuthResponse(user, refreshToken);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var existingToken = await context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken)
                ?? throw new BusinessRuleException("Invalid refresh token.");

            if (!existingToken.IsActive)
            {
                throw new BusinessRuleException("Refresh token is expired or has been revoked.");
            }

            var user = existingToken.User ?? throw new BusinessRuleException("Invalid refresh token.");
            if (!user.IsActive) throw new BusinessRuleException("Your account has been deactivated.");

            var newRefreshToken = GenerateRefreshToken(user.Id);
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReplacedByToken = newRefreshToken.Token;

            context.RefreshTokens.Add(newRefreshToken);
            
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error rotating refresh token for user {UserId}", user.Id);
                throw new BusinessRuleException("Failed to refresh token.");
            }

            logger.LogInformation("Rotated refresh token for user {UserId}", user.Id);
            return BuildAuthResponse(user, newRefreshToken);

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

            var refreshToken = GenerateRefreshToken(user.Id);
            context.RefreshTokens.Add(refreshToken);

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
            return BuildAuthResponse(user, refreshToken);
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var existingToken = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken)
                ?? throw new BusinessRuleException("Invalid refresh token.");

            if (!existingToken.IsActive) return;

            existingToken.RevokedAt = DateTime.UtcNow;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error revoking refresh token.");
                throw new BusinessRuleException("Failed to revoke token.");
            }

            logger.LogInformation("Revoked refresh token {TokenId}", existingToken.Id);
        }

        private static RefreshToken GenerateRefreshToken(Guid userId)
        {
            return new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
        }

        private AuthResponse BuildAuthResponse(User user, RefreshToken refreshToken)
        {
            var (token, expiresAt) = jwtGenerator.GenerateToken(user);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }
    }
}
