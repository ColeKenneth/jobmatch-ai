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
    public sealed class EmploymentHistoryServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly EmploymentHistoryService _service;

        public EmploymentHistoryServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new EmploymentHistoryService(_context, Mock.Of<ILogger<EmploymentHistoryService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Alumni> CreateTestAlumniAsync()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"alumni_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = UserRole.Alumni
            };
            var alumnus = new Alumni
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 5, 15)
            };

            _context.Users.Add(user);
            _context.Alumni.Add(alumnus);
            await _context.SaveChangesAsync();
            return alumnus;
        }

        [Fact]
        public async Task CreateAsync_ShouldAddEmploymentHistory_WhenAlumniExistsAndValid()
        {
            var alumnus = await CreateTestAlumniAsync();

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Software Engineer",
                StartDate = new DateOnly(2024, 6, 1),
                IsCurrent = true,
                Responsibilities = "Writing backend code"
            };

            var result = await _service.CreateAsync(alumnus.Id, dto);

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.AlumniId.Should().Be(alumnus.Id);
            result.CompanyName.Should().Be("Tech Corp");
            result.IsCurrent.Should().BeTrue();

            var saved = await _context.EmploymentHistories.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenAlumniDoesNotExist()
        {
            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Developer",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.CreateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenCurrentPositionHasEndDate()
        {
            var alumnus = await CreateTestAlumniAsync();

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Developer",
                StartDate = new DateOnly(2024, 1, 1),
                EndDate = new DateOnly(2024, 12, 31),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.CreateAsync(alumnus.Id, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*current position cannot have an end date*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenPastPositionLacksEndDate()
        {
            var alumnus = await CreateTestAlumniAsync();

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Developer",
                StartDate = new DateOnly(2024, 1, 1),
                EndDate = null,
                IsCurrent = false
            };

            Func<Task> act = async () => await _service.CreateAsync(alumnus.Id, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*past position must have an end date*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenEndDateIsBeforeStartDate()
        {
            var alumnus = await CreateTestAlumniAsync();

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Developer",
                StartDate = new DateOnly(2024, 6, 1),
                EndDate = new DateOnly(2024, 1, 1),
                IsCurrent = false
            };

            Func<Task> act = async () => await _service.CreateAsync(alumnus.Id, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*End date must be on or after the start date*");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEmploymentHistory_WhenExists()
        {
            var alumnus = await CreateTestAlumniAsync();
            var history = new EmploymentHistory
            {
                Id = Guid.NewGuid(),
                AlumniId = alumnus.Id,
                CompanyName = "Tech Corp",
                Position = "Developer",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };
            _context.EmploymentHistories.Add(history);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(history.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.EmploymentHistories.FindAsync(history.Id);
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
        public async Task ExistsAsync_ShouldReturnTrue_WhenEmploymentHistoryExists()
        {
            var alumnus = await CreateTestAlumniAsync();
            var history = new EmploymentHistory
            {
                Id = Guid.NewGuid(),
                AlumniId = alumnus.Id,
                CompanyName = "Tech Corp",
                Position = "Developer",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };
            _context.EmploymentHistories.Add(history);
            await _context.SaveChangesAsync();

            var result = await _service.ExistsAsync(history.Id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenEmploymentHistoryDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByAlumniAsync_ShouldReturnOrderedEmploymentHistories_WhenAlumniExists()
        {
            var alumnus = await CreateTestAlumniAsync();
            _context.EmploymentHistories.AddRange(
                new EmploymentHistory { Id = Guid.NewGuid(), AlumniId = alumnus.Id, CompanyName = "Old Co", Position = "Dev", StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2022, 1, 1), IsCurrent = false },
                new EmploymentHistory { Id = Guid.NewGuid(), AlumniId = alumnus.Id, CompanyName = "New Co", Position = "Senior Dev", StartDate = new DateOnly(2023, 1, 1), IsCurrent = true }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetByAlumniAsync(alumnus.Id)).ToList();

            results.Should().HaveCount(2);
            results.First().CompanyName.Should().Be("New Co");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEmploymentHistoryDto_WhenExists()
        {
            var alumnus = await CreateTestAlumniAsync();
            var history = new EmploymentHistory
            {
                Id = Guid.NewGuid(),
                AlumniId = alumnus.Id,
                CompanyName = "Tech Corp",
                Position = "Developer",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };
            _context.EmploymentHistories.Add(history);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(history.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(history.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEmploymentHistory_WhenValid()
        {
            var alumnus = await CreateTestAlumniAsync();
            var history = new EmploymentHistory
            {
                Id = Guid.NewGuid(),
                AlumniId = alumnus.Id,
                CompanyName = "Old Corp",
                Position = "Dev",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };
            _context.EmploymentHistories.Add(history);
            await _context.SaveChangesAsync();

            var updateDto = new EmploymentHistoryDto
            {
                AlumniId = alumnus.Id,
                CompanyName = "Updated Corp",
                Position = "Senior Dev",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };

            await _service.UpdateAsync(history.Id, updateDto);

            var updated = await _context.EmploymentHistories.FindAsync(history.Id);
            updated.Should().NotBeNull();
            updated!.CompanyName.Should().Be("Updated Corp");
            updated.Position.Should().Be("Senior Dev");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenEmploymentHistoryDoesNotExist()
        {
            var updateDto = new EmploymentHistoryDto
            {
                CompanyName = "Corp",
                Position = "Dev",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), updateDto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenReassigningAlumniId()
        {
            var alumnus1 = await CreateTestAlumniAsync();
            var alumnus2 = await CreateTestAlumniAsync();

            var history = new EmploymentHistory
            {
                Id = Guid.NewGuid(),
                AlumniId = alumnus1.Id,
                CompanyName = "Corp",
                Position = "Dev",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };
            _context.EmploymentHistories.Add(history);
            await _context.SaveChangesAsync();

            var updateDto = new EmploymentHistoryDto
            {
                AlumniId = alumnus2.Id,
                CompanyName = "Corp",
                Position = "Dev",
                StartDate = new DateOnly(2024, 1, 1),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.UpdateAsync(history.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign employment history*");
        }
    }
}