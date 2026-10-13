using FluentAssertions;
using JobMatchAI.Application.Authentication.DTOs;
using JobMatchAI.Application.Authentication.Services;
using JobMatchAI.Application.Authentication.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using JobMatchAI.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobMatchAI.Tests.IntegrationTests.Services
{
    public sealed class AuthServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly Mock<IJwtTokenGenerator> _jwtGeneratorMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _passwordHasher = new Argon2PasswordHasher(Mock.Of<ILogger<Argon2PasswordHasher>>());
            _jwtGeneratorMock = new Mock<IJwtTokenGenerator>();

            _jwtGeneratorMock
                .Setup(j => j.GenerateToken(It.IsAny<User>()))
                .Returns(("mock_jwt_token_12345", DateTime.UtcNow.AddHours(1)));

            _service = new AuthService(
                _context,
                _passwordHasher,
                _jwtGeneratorMock.Object,
                Mock.Of<ILogger<AuthService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUserAndReturnAuthResponse_WhenValidRequest()
        {
            var request = new RegisterRequest
            {
                FirstName = "Cole Kenneth",
                MiddleName = "Barona",
                LastName = "Silva",
                Email = "cole.silva@test.com",
                Password = "SecurePassword123!",
                Role = UserRole.Student
            };

            var response = await _service.RegisterAsync(request);

            response.Should().NotBeNull();
            response.UserId.Should().NotBeEmpty();
            response.Email.Should().Be("cole.silva@test.com");
            response.FullName.Should().Be("Cole Kenneth Barona Silva");
            response.Role.Should().Be("Student");
            response.Token.Should().Be("mock_jwt_token_12345");
            response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

            var savedUser = await _context.Users.FindAsync(response.UserId);
            savedUser.Should().NotBeNull();
            savedUser!.IsActive.Should().BeTrue();
            _passwordHasher.Verify("SecurePassword123!", savedUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowAlreadyExistsException_WhenEmailAlreadyExists()
        {
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Existing",
                LastName = "User",
                Email = "existing@test.com",
                PasswordHash = _passwordHasher.Hash("Password123!"),
                Role = UserRole.Student,
                IsActive = true
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = new RegisterRequest
            {
                FirstName = "Another",
                LastName = "User",
                Email = "  EXISTING@test.com  ", 
                Password = "SecurePassword123!",
                Role = UserRole.Student
            };

            Func<Task> act = async () => await _service.RegisterAsync(request);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already registered*");
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
        {
            var password = "SecurePassword123!";
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Cole",
                LastName = "Silva",
                Email = "cole.login@test.com",
                PasswordHash = _passwordHasher.Hash(password),
                Role = UserRole.Student,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginRequest
            {
                Email = "  COLE.LOGIN@test.com  ", 
                Password = password
            };

            var response = await _service.LoginAsync(request);

            response.Should().NotBeNull();
            response.UserId.Should().Be(user.Id);
            response.Email.Should().Be("cole.login@test.com");
            response.Token.Should().Be("mock_jwt_token_12345");
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowBusinessRuleException_WhenEmailNotFound()
        {
            var request = new LoginRequest
            {
                Email = "notfound@test.com",
                Password = "SomePassword123!"
            };

            Func<Task> act = async () => await _service.LoginAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Invalid email or password*");
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowBusinessRuleException_WhenPasswordIsInvalid()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Cole",
                LastName = "Silva",
                Email = "cole.wrongpass@test.com",
                PasswordHash = _passwordHasher.Hash("CorrectPassword123!"),
                Role = UserRole.Student,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginRequest
            {
                Email = "cole.wrongpass@test.com",
                Password = "WrongPassword123!"
            };

            Func<Task> act = async () => await _service.LoginAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Invalid email or password*");
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowBusinessRuleException_WhenAccountIsDeactivated()
        {
            var password = "SecurePassword123!";
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Cole",
                LastName = "Silva",
                Email = "cole.inactive@test.com",
                PasswordHash = _passwordHasher.Hash(password),
                Role = UserRole.Student,
                IsActive = false 
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var request = new LoginRequest
            {
                Email = "cole.inactive@test.com",
                Password = password
            };

            Func<Task> act = async () => await _service.LoginAsync(request);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*deactivated*");
        }
    }
}