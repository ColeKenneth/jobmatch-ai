using FluentAssertions;
using JobMatchAI.Application.Authentication.DTOs;
using JobMatchAI.Application.Authentication.Interfaces;
using JobMatchAI.Application.Authentication.Services;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using JobMatchAI.Tests.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobMatchAI.Tests.UnitTests.Services
{
    public class AuthServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IJwtTokenGenerator> _jwtGeneratorMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _service;

        private readonly List<User> _users = [];
        private readonly List<RefreshToken> _refreshTokens = [];

        public AuthServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);

            _passwordHasherMock = new Mock<IPasswordHasher>();
            _jwtGeneratorMock = new Mock<IJwtTokenGenerator>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            var usersDbSet = DbSetMockHelper.CreateMockDbSet(_users.AsQueryable());
            var refreshTokensDbSet = DbSetMockHelper.CreateMockDbSet(_refreshTokens.AsQueryable());

            _contextMock.Setup(c => c.Users).Returns(usersDbSet);
            _contextMock.Setup(c => c.RefreshTokens).Returns(refreshTokensDbSet);

            _jwtGeneratorMock
                .Setup(j => j.GenerateToken(It.IsAny<User>()))
                .Returns(("mock_jwt_token", DateTime.UtcNow.AddHours(1)));

            _service = new AuthService(
                _contextMock.Object,
                _passwordHasherMock.Object,
                _jwtGeneratorMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUserAndReturnAuthResponse_WhenEmailIsUnique()
        {
            var request = new RegisterRequest
            {
                FirstName = "Cole Kenneth",
                LastName = "Silva",
                Email = "cole.silva@test.com",
                Password = "SecurePassword123!",
                Role = UserRole.Student
            };

            _passwordHasherMock
                .Setup(h => h.Hash(request.Password))
                .Returns("hashed_password_abc");

            var response = await _service.RegisterAsync(request);

            response.Should().NotBeNull();
            response.Email.Should().Be("cole.silva@test.com");
            response.FullName.Should().Be("Cole Kenneth Silva");
            response.Role.Should().Be("Student");
            response.Token.Should().Be("mock_jwt_token");
            response.RefreshToken.Should().NotBeNullOrEmpty();

            _contextMock.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Once);
            _contextMock.Verify(c => c.RefreshTokens.Add(It.IsAny<RefreshToken>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowAlreadyExistsException_WhenEmailAlreadyExists()
        {
            _users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "existing@test.com",
                FirstName = "Existing",
                LastName = "User",
                PasswordHash = "hash"
            });

            var request = new RegisterRequest
            {
                FirstName = "Cole",
                LastName = "Silva",
                Email = "  EXISTING@test.com  ",
                Password = "Password123!",
                Role = UserRole.Student
            };

            Func<Task> act = async () => await _service.RegisterAsync(request);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already registered*");

            _contextMock.Verify(c => c.Users.Add(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User
            {
                Id = userId,
                Email = "cole.login@test.com",
                FirstName = "Cole",
                LastName = "Silva",
                PasswordHash = "valid_hash",
                IsActive = true,
                Role = UserRole.Student
            });

            var request = new LoginRequest
            {
                Email = "COLE.LOGIN@test.com",
                Password = "CorrectPassword123!"
            };

            _passwordHasherMock
                .Setup(h => h.Verify(request.Password, "valid_hash"))
                .Returns(true);

            var response = await _service.LoginAsync(request);

            response.Should().NotBeNull();
            response.UserId.Should().Be(userId);
            response.Email.Should().Be("cole.login@test.com");
            response.Token.Should().Be("mock_jwt_token");

            _contextMock.Verify(c => c.RefreshTokens.Add(It.IsAny<RefreshToken>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowBusinessRuleException_WhenPasswordIsInvalid()
        {
            _users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "cole.login@test.com",
                FirstName = "Cole",
                LastName = "Silva",
                PasswordHash = "valid_hash",
                IsActive = true
            });

            var request = new LoginRequest
            {
                Email = "cole.login@test.com",
                Password = "WrongPassword!"
            };

            _passwordHasherMock
                .Setup(h => h.Verify(request.Password, "valid_hash"))
                .Returns(false);

            Func<Task> act = async () => await _service.LoginAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Invalid email or password*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNewAuthResponse_WhenTokenIsValid()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "cole@test.com",
                FirstName = "Cole",
                LastName = "Silva",
                IsActive = true,
                Role = UserRole.Student
            };
            _users.Add(user);

            var existingToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                Token = "valid_refresh_token_string",
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                RevokedAt = null
            };
            _refreshTokens.Add(existingToken);

            var response = await _service.RefreshTokenAsync("valid_refresh_token_string");

            response.Should().NotBeNull();
            response.Token.Should().Be("mock_jwt_token");
            response.RefreshToken.Should().NotBe("valid_refresh_token_string");

            existingToken.RevokedAt.Should().NotBeNull();
            existingToken.ReplacedByToken.Should().Be(response.RefreshToken);

            _contextMock.Verify(c => c.RefreshTokens.Add(It.IsAny<RefreshToken>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldThrowBusinessRuleException_WhenTokenIsRevoked()
        {
            var existingToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "revoked_token",
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                RevokedAt = DateTime.UtcNow.AddDays(-1)
            };
            _refreshTokens.Add(existingToken);

            Func<Task> act = async () => await _service.RefreshTokenAsync("revoked_token");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*expired or has been revoked*");
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldRevokeToken_WhenTokenIsValid()
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "token_to_revoke",
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                RevokedAt = null
            };
            _refreshTokens.Add(token);

            await _service.RevokeTokenAsync("token_to_revoke");

            token.RevokedAt.Should().NotBeNull();
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldThrowBusinessRuleException_WhenTokenIsNotFound()
        {
            Func<Task> act = async () => await _service.RevokeTokenAsync("non_existent_token");

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Invalid refresh token*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}