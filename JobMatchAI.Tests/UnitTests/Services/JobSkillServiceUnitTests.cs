using FluentAssertions;
using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Implementations;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobMatchAI.Tests.UnitTests.Services
{
    public class JobSkillServiceUnitTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<JobSkillService>> _loggerMock;
        private readonly JobSkillService _service;

        private readonly Guid _employerId = Guid.NewGuid();
        private readonly Guid _jobPostingId = Guid.NewGuid();
        private readonly Guid _skillId = Guid.NewGuid();

        public JobSkillServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _loggerMock = new Mock<ILogger<JobSkillService>>();
            _service = new JobSkillService(_context, _loggerMock.Object);

            SeedDatabase().GetAwaiter().GetResult();
        }

        private async Task SeedDatabase()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Employer",
                Email = "employer@test.com",
                PasswordHash = "hash",
                Role = Domain.Enums.UserRole.Employer,
                IsActive = true
            };

            var employer = new Employer
            {
                Id = _employerId,
                UserId = user.Id,
                CompanyName = "Test Corp",
                Industry = "IT"
            };

            var jobPosting = new JobPosting
            {
                Id = _jobPostingId,
                EmployerId = _employerId,
                Title = "Software Engineer",
                Description = "Test job",
                Requirements = "Test requirements",
                Location = "Manila",
                EmploymentType = Domain.Enums.EmploymentType.FullTime
            };

            var skill = new Skill
            {
                Id = _skillId,
                Name = "C#",
                Category = Domain.Enums.Category.Technical
            };

            _context.Users.Add(user);
            _context.Employers.Add(employer);
            _context.JobPostings.Add(jobPosting);
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task AddSkillAsync_ReturnsJobSkillDto_WhenValidRequest()
        {
            var dto = new JobSkillDto
            {
                SkillId = _skillId,
                IsRequired = true,
                MinimumProficiencyLevel = 3,
                Priority = 1
            };

            var result = await _service.AddSkillAsync(_jobPostingId, dto);

            result.Should().NotBeNull();
            result.JobPostingId.Should().Be(_jobPostingId);
            result.SkillId.Should().Be(_skillId);
            result.IsRequired.Should().BeTrue();
            result.MinimumProficiencyLevel.Should().Be(3);
            result.Priority.Should().Be(1);
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task AddSkillAsync_ThrowsNotFoundException_WhenJobPostingDoesNotExist()
        {
            var dto = new JobSkillDto { SkillId = _skillId };
            var nonExistentJobId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.AddSkillAsync(nonExistentJobId, dto));
        }

        [Fact]
        public async Task AddSkillAsync_ThrowsNotFoundException_WhenSkillDoesNotExist()
        {
            var dto = new JobSkillDto { SkillId = Guid.NewGuid() };

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.AddSkillAsync(_jobPostingId, dto));
        }

        [Fact]
        public async Task AddSkillAsync_ThrowsAlreadyExistsException_WhenSkillAlreadyAttached()
        {
            var dto = new JobSkillDto { SkillId = _skillId };
            await _service.AddSkillAsync(_jobPostingId, dto);

            await Assert.ThrowsAsync<AlreadyExistsException>(
                () => _service.AddSkillAsync(_jobPostingId, dto));
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenJobSkillExists()
        {
            await _service.AddSkillAsync(_jobPostingId, new JobSkillDto { SkillId = _skillId });

            var result = await _service.ExistsAsync(_jobPostingId, _skillId);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalse_WhenJobSkillDoesNotExist()
        {
            var result = await _service.ExistsAsync(_jobPostingId, Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenExists()
        {
            var created = await _service.AddSkillAsync(_jobPostingId, new JobSkillDto { SkillId = _skillId });

            var result = await _service.GetByIdAsync(created.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(created.Id);
            result.SkillId.Should().Be(_skillId);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByJobPostingAsync_ReturnsAllSkillsForJob()
        {
            var skill2 = new Skill
            {
                Id = Guid.NewGuid(),
                Name = "Python",
                Category = Domain.Enums.Category.Technical
            };
            _context.Skills.Add(skill2);
            await _context.SaveChangesAsync();

            await _service.AddSkillAsync(_jobPostingId, new JobSkillDto { SkillId = _skillId, Priority = 1 });
            await _service.AddSkillAsync(_jobPostingId, new JobSkillDto { SkillId = skill2.Id, Priority = 5 });

            var result = await _service.GetByJobPostingAsync(_jobPostingId);

            result.Should().HaveCount(2);
            result.First().Priority.Should().Be(5);
        }

        [Fact]
        public async Task GetByJobPostingAsync_ReturnsEmpty_WhenNoSkillsAttached()
        {
            var result = await _service.GetByJobPostingAsync(_jobPostingId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateSkillAsync_UpdatesFields_WhenValidRequest()
        {
            var created = await _service.AddSkillAsync(_jobPostingId, new JobSkillDto
            {
                SkillId = _skillId,
                IsRequired = true,
                MinimumProficiencyLevel = 2,
                Priority = 3
            });

            var updateDto = new JobSkillDto
            {
                SkillId = _skillId,
                IsRequired = false,
                MinimumProficiencyLevel = 5,
                Priority = 1
            };

            await _service.UpdateSkillAsync(created.Id, updateDto);
            var updated = await _service.GetByIdAsync(created.Id);

            updated!.IsRequired.Should().BeFalse();
            updated.MinimumProficiencyLevel.Should().Be(5);
            updated.Priority.Should().Be(1);
        }

        [Fact]
        public async Task UpdateSkillAsync_ThrowsNotFoundException_WhenDoesNotExist()
        {
            var dto = new JobSkillDto { SkillId = _skillId };

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.UpdateSkillAsync(Guid.NewGuid(), dto));
        }
    }
}