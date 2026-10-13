using FluentAssertions;
using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Implementations;
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
    public class UserServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _service;

        private readonly List<User> _users = [];

        public UserServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);
            _loggerMock = new Mock<ILogger<UserService>>();

            _contextMock.Setup(c => c.Users).Returns(DbSetMockHelper.CreateMockDbSet(_users));
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _service = new UserService(_contextMock.Object, _loggerMock.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _users.Add(new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Cole",
                LastName = "Silva",
                Email = "cole.silva@test.com",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true
            });

            _users.Add(new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                MiddleName = "Doe",
                LastName = "Smith",
                Email = "jane.smith@test.com",
                PasswordHash = "hash",
                Role = UserRole.Employer,
                IsActive = true
            });
        }

        [Fact]
        public async Task CreateAsync_ReturnsDto_WhenValidRequest()
        {
            var dto = new UserDto
            {
                FirstName = "Mark",
                LastName = "Reyes",
                Email = "mark.reyes@test.com",
                Role = UserRole.Student,
                IsActive = true
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.FirstName.Should().Be("Mark");
            result.LastName.Should().Be("Reyes");
            result.Email.Should().Be("mark.reyes@test.com");
            result.Role.Should().Be(UserRole.Student);
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateAsync_NormalizesEmail_ToLowerCase()
        {
            var dto = new UserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "  MARK.REYES@TEST.COM  ",
                Role = UserRole.Student
            };

            var result = await _service.CreateAsync(dto);

            result.Email.Should().Be("mark.reyes@test.com");
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenEmailExists()
        {
            var dto = new UserDto
            {
                FirstName = "Cole",
                LastName = "Silva",
                Email = "cole.silva@test.com",
                Role = UserRole.Student
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenEmailDiffersOnlyByCase()
        {
            var dto = new UserDto
            {
                FirstName = "Cole",
                LastName = "Silva",
                Email = "COLE.SILVA@TEST.COM",
                Role = UserRole.Student
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task EmailExistsAsync_ReturnsTrue_WhenExists()
        {
            var result = await _service.EmailExistsAsync("cole.silva@test.com");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task EmailExistsAsync_IsCaseInsensitive()
        {
            var result = await _service.EmailExistsAsync("COLE.SILVA@TEST.COM");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task EmailExistsAsync_ReturnsFalse_WhenDoesNotExist()
        {
            var result = await _service.EmailExistsAsync("nobody@test.com");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenExists()
        {
            var id = _users[0].Id;

            var result = await _service.ExistsAsync(id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalse_WhenDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsersOrderedByLastName()
        {
            var result = (await _service.GetAllAsync()).ToList();

            result.Should().HaveCount(2);
            result[0].LastName.Should().Be("Silva");
            result[1].LastName.Should().Be("Smith");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmpty_WhenNoUsers()
        {
            _users.Clear();

            var result = await _service.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsDto_WhenExists()
        {
            var result = await _service.GetByEmailAsync("cole.silva@test.com");

            result.Should().NotBeNull();
            result!.Email.Should().Be("cole.silva@test.com");
        }

        [Fact]
        public async Task GetByEmailAsync_IsCaseInsensitive()
        {
            var result = await _service.GetByEmailAsync("COLE.SILVA@TEST.COM");

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByEmailAsync("nobody@test.com");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenExists()
        {
            var id = _users[0].Id;

            var result = await _service.GetByIdAsync(id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.Email.Should().Be("cole.silva@test.com");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields_WhenValidRequest()
        {
            var user = _users[0];

            var dto = new UserDto
            {
                FirstName = "Cole",
                LastName = "Silva",
                Email = "cole.silva@test.com",
                Role = UserRole.Alumni,
                IsActive = true
            };

            await _service.UpdateAsync(user.Id, dto);

            var updated = _users.First(u => u.Id == user.Id);
            updated.Role.Should().Be(UserRole.Alumni);
            updated.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_NormalizesEmail_ToLowerCase()
        {
            var user = _users[0];

            var dto = new UserDto
            {
                FirstName = "Cole",
                LastName = "Silva",
                Email = "COLE.SILVA@TEST.COM",
                Role = UserRole.Student,
                IsActive = true
            };

            await _service.UpdateAsync(user.Id, dto);

            var updated = _users.First(u => u.Id == user.Id);
            updated.Email.Should().Be("cole.silva@test.com");
        }

        [Fact]
        public async Task UpdateAsync_ThrowsAlreadyExistsException_WhenEmailConflicts()
        {
            var user = _users[0];

            var dto = new UserDto
            {
                FirstName = "Cole",
                LastName = "Silva",
                Email = "jane.smith@test.com",
                Role = UserRole.Student,
                IsActive = true
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.UpdateAsync(user.Id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsNotFoundException_WhenDoesNotExist()
        {
            var dto = new UserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@test.com",
                Role = UserRole.Student
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), dto));
        }
    }
}