using FluentAssertions;
using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Implementations;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using JobMatchAI.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobMatchAI.Tests.IntegrationTests.Services
{
    public class AlumniServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AlumniService _service;

        public AlumniServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new AlumniService(_context, Mock.Of<ILogger<AlumniService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<User> CreateTestUserAsync(UserRole role = UserRole.Alumni)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"alumni_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task CreateAsync_ShouldAddAlumni_WhenUserIsAlumniRoleAndValid()
        {
            var user = await CreateTestUserAsync(UserRole.Alumni);

            var dto = new AlumniDto
            {
                UserId = user.Id,
                Program = "BS Information Technology",
                GraduationDate = new DateOnly(2025, 5, 15),
                CurrentEmployer = "Tech Corp",
                CurrentPosition = "Software Engineer"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.UserId.Should().Be(user.Id);
            result.Program.Should().Be("BS Information Technology");
            result.GraduationYear.Should().Be(2025);
            result.CurrentEmployer.Should().Be("Tech Corp");

            var saved = await _context.Alumni.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            var dto = new AlumniDto
            {
                UserId = Guid.NewGuid(),
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenUserIsNotAlumniRole()
        {
            var user = await CreateTestUserAsync(UserRole.Student);

            var dto = new AlumniDto
            {
                UserId = user.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*alumni role*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenAlumniProfileAlreadyExistsForUser()
        {
            var user = await CreateTestUserAsync(UserRole.Alumni);

            var existing = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2023, 1, 1)
            };
            _context.Alumni.Add(existing);
            await _context.SaveChangesAsync();

            var dto = new AlumniDto
            {
                UserId = user.Id,
                Program = "BS Computer Science",
                GraduationDate = new DateOnly(2024, 1, 1)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveAlumni_WhenExists()
        {
            var user = await CreateTestUserAsync(UserRole.Alumni);
            var alumnus = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };
            _context.Alumni.Add(alumnus);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(alumnus.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.Alumni.FindAsync(alumnus.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            Func<Task> act = async () => await _service.DeleteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenAlumniExists()
        {
            var user = await CreateTestUserAsync(UserRole.Alumni);
            var alumnus = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };
            _context.Alumni.Add(alumnus);
            await _context.SaveChangesAsync();

            var result = await _service.ExistsAsync(alumnus.Id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenAlumniDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnOrderedAlumni_WhenProfilesExist()
        {
            var user1 = await CreateTestUserAsync(UserRole.Alumni);
            var user2 = await CreateTestUserAsync(UserRole.Alumni);

            _context.Alumni.AddRange(
                new Alumni { Id = Guid.NewGuid(), UserId = user1.Id, Program = "BSIT", GraduationDate = new DateOnly(2025, 1, 1) },
                new Alumni { Id = Guid.NewGuid(), UserId = user2.Id, Program = "BSCS", GraduationDate = new DateOnly(2023, 1, 1) }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetAllAsync()).ToList();

            results.Should().HaveCount(2);
            results.First().GraduationDate.Should().Be(new DateOnly(2023, 1, 1));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnAlumniDto_WhenExists()
        {
            var user = await CreateTestUserAsync(UserRole.Alumni);
            var alumnus = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };
            _context.Alumni.Add(alumnus);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(alumnus.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(alumnus.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnAlumniDto_WhenUserIdExists()
        {
            var user = await CreateTestUserAsync(UserRole.Alumni);
            var alumnus = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };
            _context.Alumni.Add(alumnus);
            await _context.SaveChangesAsync();

            var result = await _service.GetByUserIdAsync(user.Id);

            result.Should().NotBeNull();
            result!.UserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnNull_WhenUserIdDoesNotExist()
        {
            var result = await _service.GetByUserIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateAlumni_WhenValid()
        {
            var user = await CreateTestUserAsync(UserRole.Alumni);
            var alumnus = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Program = "Old Program",
                GraduationDate = new DateOnly(2024, 1, 1),
                CurrentEmployer = "Old Corp",
                CurrentPosition = "Junior Dev"
            };
            _context.Alumni.Add(alumnus);
            await _context.SaveChangesAsync();

            var updateDto = new AlumniDto
            {
                UserId = user.Id,
                Program = "Updated Program",
                GraduationDate = new DateOnly(2024, 6, 1),
                CurrentEmployer = "New Corp",
                CurrentPosition = "Senior Dev"
            };

            await _service.UpdateAsync(alumnus.Id, updateDto);

            var updated = await _context.Alumni.FindAsync(alumnus.Id);
            updated.Should().NotBeNull();
            updated!.Program.Should().Be("Updated Program");
            updated.CurrentEmployer.Should().Be("New Corp");
            updated.CurrentPosition.Should().Be("Senior Dev");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenAlumniDoesNotExist()
        {
            var updateDto = new AlumniDto
            {
                UserId = Guid.NewGuid(),
                Program = "Program",
                GraduationDate = new DateOnly(2024, 1, 1)
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), updateDto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenReassigningUser()
        {
            var user1 = await CreateTestUserAsync(UserRole.Alumni);
            var user2 = await CreateTestUserAsync(UserRole.Alumni);

            var alumnus = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };
            _context.Alumni.Add(alumnus);
            await _context.SaveChangesAsync();

            var updateDto = new AlumniDto
            {
                UserId = user2.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 1, 1)
            };

            Func<Task> act = async () => await _service.UpdateAsync(alumnus.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign alumni profile*");
        }
    }
}