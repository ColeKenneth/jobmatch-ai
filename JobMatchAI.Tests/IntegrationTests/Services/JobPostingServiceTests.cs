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
    public sealed class JobPostingServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly JobPostingService _service;

        public JobPostingServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new JobPostingService(_context, Mock.Of<ILogger<JobPostingService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Employer> CreateTestEmployerAsync(string email = "employer@test.com", string companyName = "Tech Corp")
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = "hash",
                Role = UserRole.Employer
            };

            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = companyName,
                Industry = "Software"
            };

            _context.Users.Add(user);
            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();
            return employer;
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateJobPosting_WhenValidDtoAndEmployerExists()
        {
            var employer = await CreateTestEmployerAsync();
            var dto = new JobPostingDto
            {
                EmployerId = employer.Id,
                Title = "Software Engineer",
                Description = "Develop backend systems",
                Requirements = "C# and SQL",
                Location = "Quezon City",
                EmploymentType = EmploymentType.FullTime,
                MinSalary = 50000m,
                MaxSalary = 80000m,
                Deadline = DateTime.UtcNow.AddDays(30)
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.EmployerId.Should().Be(employer.Id);
            result.Title.Should().Be("Software Engineer");
            result.IsActive.Should().BeTrue();

            var saved = await _context.JobPostings.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenEmployerDoesNotExist()
        {
            var dto = new JobPostingDto
            {
                EmployerId = Guid.NewGuid(),
                Title = "Software Engineer",
                Description = "Develop systems",
                Requirements = "C#",
                Location = "Quezon City"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Employer not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenMinSalaryGreaterThanMaxSalary()
        {
            var employer = await CreateTestEmployerAsync();
            var dto = new JobPostingDto
            {
                EmployerId = employer.Id,
                Title = "Software Engineer",
                Description = "Develop systems",
                Requirements = "C#",
                Location = "Quezon City",
                MinSalary = 90000m,
                MaxSalary = 60000m
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Minimum salary cannot be greater than the maximum salary*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenDeadlineIsInThePast()
        {
            var employer = await CreateTestEmployerAsync();
            var dto = new JobPostingDto
            {
                EmployerId = employer.Id,
                Title = "Software Engineer",
                Description = "Develop systems",
                Requirements = "C#",
                Location = "Quezon City",
                Deadline = DateTime.UtcNow.AddDays(-1)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Deadline must be in the future*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnJobPostingDto_WhenExists()
        {
            var employer = await CreateTestEmployerAsync();
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Backend Developer",
                Description = "API development",
                Requirements = "Java Spring Boot",
                Location = "Makati",
                EmploymentType = EmploymentType.FullTime
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(jobPosting.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(jobPosting.Id);
            result.Title.Should().Be("Backend Developer");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByLocationAsync_ShouldReturnMatchingJobPostings_CaseInsensitive()
        {
            var employer = await CreateTestEmployerAsync();
            _context.JobPostings.AddRange(
                new JobPosting { Id = Guid.NewGuid(), EmployerId = employer.Id, Title = "Job 1", Description = "Desc", Requirements = "Req", Location = "Manila", EmploymentType = EmploymentType.FullTime },
                new JobPosting { Id = Guid.NewGuid(), EmployerId = employer.Id, Title = "Job 2", Description = "Desc", Requirements = "Req", Location = "manila", EmploymentType = EmploymentType.FullTime }
            );
            await _context.SaveChangesAsync();

            var results = await _service.GetByLocationAsync("MANILA");

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateJobPosting_WhenValid()
        {
            var employer = await CreateTestEmployerAsync();
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Old Title",
                Description = "Old Desc",
                Requirements = "Old Req",
                Location = "Quezon City",
                EmploymentType = EmploymentType.FullTime
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            var updateDto = new JobPostingDto
            {
                EmployerId = employer.Id,
                Title = "New Title",
                Description = "New Desc",
                Requirements = "New Req",
                Location = "Pasig",
                EmploymentType = EmploymentType.PartTime,
                IsActive = true
            };

            await _service.UpdateAsync(jobPosting.Id, updateDto);

            var updated = await _context.JobPostings.FindAsync(jobPosting.Id);
            updated.Should().NotBeNull();
            updated!.Title.Should().Be("New Title");
            updated.Location.Should().Be("Pasig");
            updated.EmploymentType.Should().Be(EmploymentType.PartTime);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenJobPostingDoesNotExist()
        {
            var dto = new JobPostingDto
            {
                Title = "Title",
                Description = "Desc",
                Requirements = "Req",
                Location = "Location"
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Job posting*not found*");
        }

        [Fact]
        public async Task CloseAsync_ShouldSetIsActiveToFalse_WhenExists()
        {
            var employer = await CreateTestEmployerAsync();
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Job",
                Description = "Desc",
                Requirements = "Req",
                Location = "Location",
                IsActive = true
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            await _service.CloseAsync(jobPosting.Id);

            _context.ChangeTracker.Clear();
            var closed = await _context.JobPostings.FindAsync(jobPosting.Id);
            closed.Should().NotBeNull();
            closed!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ReopenAsync_ShouldSetIsActiveToTrue_WhenExists()
        {
            var employer = await CreateTestEmployerAsync();
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Job",
                Description = "Desc",
                Requirements = "Req",
                Location = "Location",
                IsActive = false
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            await _service.ReopenAsync(jobPosting.Id);

            _context.ChangeTracker.Clear();
            var reopened = await _context.JobPostings.FindAsync(jobPosting.Id);
            reopened.Should().NotBeNull();
            reopened!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task IncrementViewsAsync_ShouldIncrementViewsCount_WhenExists()
        {
            var employer = await CreateTestEmployerAsync();
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Job",
                Description = "Desc",
                Requirements = "Req",
                Location = "Location",
                Views = 5
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            await _service.IncrementViewsAsync(jobPosting.Id);

            _context.ChangeTracker.Clear();
            var updated = await _context.JobPostings.FindAsync(jobPosting.Id);
            updated.Should().NotBeNull();
            updated!.Views.Should().Be(6);
        }

        [Fact]
        public async Task IncrementApplicationsAsync_ShouldIncrementApplicationsCount_WhenExists()
        {
            var employer = await CreateTestEmployerAsync();
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Job",
                Description = "Desc",
                Requirements = "Req",
                Location = "Location",
                Applications = 2
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            await _service.IncrementApplicationsAsync(jobPosting.Id);

            _context.ChangeTracker.Clear();
            var updated = await _context.JobPostings.FindAsync(jobPosting.Id);
            updated.Should().NotBeNull();
            updated!.Applications.Should().Be(3);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveJobPosting_WhenExists()
        {
            var employer = await CreateTestEmployerAsync();
            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Job to delete",
                Description = "Desc",
                Requirements = "Req",
                Location = "Location"
            };

            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(jobPosting.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.JobPostings.FindAsync(jobPosting.Id);
            deleted.Should().BeNull();
        }
    }
}