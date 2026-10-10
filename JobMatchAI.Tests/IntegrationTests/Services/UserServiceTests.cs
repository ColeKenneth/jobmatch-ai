using FluentAssertions;
using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Implementations;
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
    public class UserServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new UserService(_context, Mock.Of<ILogger<UserService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<User> CreateTestUserAsync(
            string? email = null,
            UserRole role = UserRole.Student,
            bool isActive = true)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Cole",
                MiddleName = "Kenneth",
                LastName = "Silva",
                Email = email ?? $"user_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = role,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateUser_WhenValidDtoAndEmailIsUnique()
        {
            var dto = new UserDto
            {
                FirstName = " Cole ",
                MiddleName = " Kenneth ",
                LastName = " Silva ",
                Email = "  cole.silva@test.com  ",
                Role = UserRole.Student,
                IsActive = true
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.FirstName.Should().Be("Cole");
            result.MiddleName.Should().Be("Kenneth");
            result.LastName.Should().Be("Silva");
            result.Email.Should().Be("cole.silva@test.com");
            result.Role.Should().Be(UserRole.Student);
            result.IsActive.Should().BeTrue();

            var saved = await _context.Users.FindAsync(result.Id);
            saved.Should().NotBeNull();
            saved!.Email.Should().Be("cole.silva@test.com");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenEmailAlreadyExistsCaseInsensitive()
        {
            await CreateTestUserAsync("john.doe@test.com");

            var dto = new UserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "JOHN.DOE@TEST.COM",
                Role = UserRole.Student
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUserDto_WhenUserExists()
        {
            var user = await CreateTestUserAsync();

            var result = await _service.GetByIdAsync(user.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            result.FullName.Should().Be("Cole Kenneth Silva");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUserDto_WhenEmailExistsCaseInsensitive()
        {
            var user = await CreateTestUserAsync("test.user@domain.com");

            var result = await _service.GetByEmailAsync("  TEST.USER@DOMAIN.COM  ");

            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.Email.Should().Be("test.user@domain.com");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnUsersOrderedByLastName()
        {
            await CreateTestUserAsync("user1@test.com", role: UserRole.Student);
            var userZ = new User { Id = Guid.NewGuid(), FirstName = "Alpha", LastName = "Zeta", Email = "a.z@test.com", Role = UserRole.Student };
            var userA = new User { Id = Guid.NewGuid(), FirstName = "Beta", LastName = "Adams", Email = "b.a@test.com", Role = UserRole.Student };

            _context.Users.AddRange(userZ, userA);
            await _context.SaveChangesAsync();

            var results = (await _service.GetAllAsync()).ToList();

            results.Count.Should().BeGreaterThanOrEqualTo(3);
            results.First().LastName.Should().Be("Adams");
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnTrue_WhenEmailExists()
        {
            await CreateTestUserAsync("check.email@test.com");

            var exists = await _service.EmailExistsAsync("CHECK.EMAIL@TEST.COM");

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task EmailExistsAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
        {
            var exists = await _service.EmailExistsAsync("nonexistent@test.com");

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUser_WhenValidDtoAndEmailIsUnique()
        {
            var user = await CreateTestUserAsync();

            var updateDto = new UserDto
            {
                FirstName = "UpdatedFirstName",
                MiddleName = null,
                LastName = "UpdatedLastName",
                Email = user.Email,
                Role = UserRole.Admin,
                IsActive = true
            };

            await _service.UpdateAsync(user.Id, updateDto);

            var updated = await _context.Users.FindAsync(user.Id);
            updated.Should().NotBeNull();
            updated!.FirstName.Should().Be("UpdatedFirstName");
            updated.MiddleName.Should().BeNull();
            updated.LastName.Should().Be("UpdatedLastName");
            updated.Role.Should().Be(UserRole.Admin);
            updated.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            var dto = new UserDto
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@test.com",
                Role = UserRole.Student
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*User*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowAlreadyExistsException_WhenEmailConflictsWithAnotherUser()
        {
            var user1 = await CreateTestUserAsync("user1@test.com");
            var user2 = await CreateTestUserAsync("user2@test.com");

            var updateDto = new UserDto
            {
                FirstName = "User2",
                LastName = "Test",
                Email = "user1@test.com", // Attempt to take user1's email
                Role = UserRole.Student
            };

            Func<Task> act = async () => await _service.UpdateAsync(user2.Id, updateDto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task ActivateAsync_ShouldSetIsActiveToTrue_WhenUserIsInactive()
        {
            var user = await CreateTestUserAsync(isActive: false);

            await _service.ActivateAsync(user.Id);

            _context.ChangeTracker.Clear();
            var activated = await _context.Users.FindAsync(user.Id);
            activated.Should().NotBeNull();
            activated!.IsActive.Should().BeTrue();
            activated.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ActivateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            Func<Task> act = async () => await _service.ActivateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*User*not found*");
        }

        [Fact]
        public async Task DeactivateAsync_ShouldSetIsActiveToFalse_WhenUserIsActive()
        {
            var user = await CreateTestUserAsync(isActive: true);

            await _service.DeactivateAsync(user.Id);

            _context.ChangeTracker.Clear();
            var deactivated = await _context.Users.FindAsync(user.Id);
            deactivated.Should().NotBeNull();
            deactivated!.IsActive.Should().BeFalse();
            deactivated.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeactivateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            Func<Task> act = async () => await _service.DeactivateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*User*not found*");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser_WhenUserExists()
        {
            var user = await CreateTestUserAsync();

            await _service.DeleteAsync(user.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.Users.FindAsync(user.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            Func<Task> act = async () => await _service.DeleteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*User*not found*");
        }
    }
}