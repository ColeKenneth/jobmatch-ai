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
    public class JobPostingServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<DbSet<Employer>> _employersDbSetMock;
        private readonly Mock<DbSet<JobPosting>> _jobPostingsDbSetMock;
        private readonly Mock<ILogger<JobPostingService>> _loggerMock;
        private readonly JobPostingService _service;

        private readonly List<Employer> _employers = [];
        private readonly List<JobPosting> _jobPostings = [];

        public JobPostingServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);

            _loggerMock = new Mock<ILogger<JobPostingService>>();

            _employersDbSetMock = new Mock<DbSet<Employer>>();
            _jobPostingsDbSetMock = new Mock<DbSet<JobPosting>>();

            var employersQueryable = _employers.AsQueryable();
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Employer>(employersQueryable.Provider));
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.Expression).Returns(employersQueryable.Expression);
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.ElementType).Returns(employersQueryable.ElementType);
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.GetEnumerator()).Returns(employersQueryable.GetEnumerator());
            _employersDbSetMock.As<IAsyncEnumerable<Employer>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Employer>(employersQueryable.GetEnumerator()));

            _employersDbSetMock
                .Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _employers.FirstOrDefault(e => e.Id == (Guid)ids[0]));

            var jobPostingsQueryable = _jobPostings.AsQueryable();
            _jobPostingsDbSetMock.As<IQueryable<JobPosting>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<JobPosting>(jobPostingsQueryable.Provider));
            _jobPostingsDbSetMock.As<IQueryable<JobPosting>>().Setup(m => m.Expression).Returns(jobPostingsQueryable.Expression);
            _jobPostingsDbSetMock.As<IQueryable<JobPosting>>().Setup(m => m.ElementType).Returns(jobPostingsQueryable.ElementType);
            _jobPostingsDbSetMock.As<IQueryable<JobPosting>>().Setup(m => m.GetEnumerator()).Returns(jobPostingsQueryable.GetEnumerator());
            _jobPostingsDbSetMock.As<IAsyncEnumerable<JobPosting>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<JobPosting>(jobPostingsQueryable.GetEnumerator()));

            _jobPostingsDbSetMock
                .Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _jobPostings.FirstOrDefault(jp => jp.Id == (Guid)ids[0]));

            _contextMock.Setup(c => c.Employers).Returns(_employersDbSetMock.Object);
            _contextMock.Setup(c => c.JobPostings).Returns(_jobPostingsDbSetMock.Object);

            _service = new JobPostingService(_contextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateJobPostingAndReturnDto_WhenEmployerExistsAndDataIsValid()
        {
            var employerId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId, CompanyName = "Tech Corp" });

            var dto = new JobPostingDto
            {
                EmployerId = employerId,
                Title = "Software Engineer",
                Description = "Build backend services.",
                Requirements = "Java, Spring Boot",
                Location = "Manila",
                EmploymentType = EmploymentType.FullTime,
                MinSalary = 50000,
                MaxSalary = 80000,
                Deadline = DateTime.UtcNow.AddDays(30)
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.Title.Should().Be("Software Engineer");
            result.EmployerId.Should().Be(employerId);

            _jobPostingsDbSetMock.Verify(d => d.Add(It.IsAny<JobPosting>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenEmployerDoesNotExist()
        {
            var employerId = Guid.NewGuid();
            var dto = new JobPostingDto
            {
                EmployerId = employerId,
                Title = "Dev",
                Description = "Desc",
                Requirements = "Req",
                Location = "Manila"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"*Employer not found: {employerId}*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenMinSalaryGreaterThanMaxSalary()
        {
            var employerId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId });

            var dto = new JobPostingDto
            {
                EmployerId = employerId,
                Title = "Dev",
                Description = "Desc",
                Requirements = "Req",
                Location = "Manila",
                MinSalary = 90000,
                MaxSalary = 50000
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Minimum salary cannot be greater than the maximum salary*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenDeadlineIsInThePast()
        {
            var employerId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId });

            var dto = new JobPostingDto
            {
                EmployerId = employerId,
                Title = "Dev",
                Description = "Desc",
                Requirements = "Req",
                Location = "Manila",
                Deadline = DateTime.UtcNow.AddDays(-1)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Deadline must be in the future*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenDbUpdateExceptionOccurs()
        {
            var employerId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId });

            var dto = new JobPostingDto
            {
                EmployerId = employerId,
                Title = "Dev",
                Description = "Desc",
                Requirements = "Req",
                Location = "Manila"
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Failed to create job posting*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnJobPostingDto_WhenJobPostingExists()
        {
            var jobId = Guid.NewGuid();
            _jobPostings.Add(new JobPosting { Id = jobId, Title = "Java Developer", Location = "Quezon City" });

            var result = await _service.GetByIdAsync(jobId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(jobId);
            result.Title.Should().Be("Java Developer");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenJobPostingDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByLocationAsync_ShouldReturnMatchingJobPostings_CaseInsensitive()
        {
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Job 1", Location = "Metro Manila" });
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Job 2", Location = "metro manila" });
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Job 3", Location = "Cebu City" });

            var result = await _service.GetByLocationAsync("METRO MANILA");

            result.Should().HaveCount(2);
            result.Select(j => j.Title).Should().Contain(["Job 1", "Job 2"]);
        }

        [Fact]
        public async Task GetActiveJobsAsync_ShouldReturnOnlyActiveAndNonExpiredJobs()
        {
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Active Job", IsActive = true, Deadline = DateTime.UtcNow.AddDays(5) });
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Inactive Job", IsActive = false, Deadline = DateTime.UtcNow.AddDays(5) });
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Expired Job", IsActive = true, Deadline = DateTime.UtcNow.AddDays(-1) });

            var result = await _service.GetActiveJobsAsync();

            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Active Job");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllJobPostings()
        {
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Job 1" });
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "Job 2" });

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByEmployerAsync_ShouldReturnJobPostingsForSpecificEmployer()
        {
            var employerId = Guid.NewGuid();
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), EmployerId = employerId, Title = "Job 1" });
            _jobPostings.Add(new JobPosting { Id = Guid.NewGuid(), EmployerId = Guid.NewGuid(), Title = "Job 2" });

            var result = await _service.GetByEmployerAsync(employerId);

            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Job 1");
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenJobPostingExists()
        {
            var jobId = Guid.NewGuid();
            _jobPostings.Add(new JobPosting { Id = jobId });

            var exists = await _service.ExistsAsync(jobId);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenJobPostingDoesNotExist()
        {
            var exists = await _service.ExistsAsync(Guid.NewGuid());

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenJobPostingDoesNotExist()
        {
            Func<Task> act = async () => await _service.DeleteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateJobPosting_WhenDataIsValid()
        {
            var jobId = Guid.NewGuid();
            var job = new JobPosting { Id = jobId, Title = "Old Title", Location = "Old Loc" };
            _jobPostings.Add(job);

            var dto = new JobPostingDto
            {
                Title = "New Title",
                Description = "Updated desc",
                Requirements = "Updated req",
                Location = "New Loc",
                IsActive = true
            };

            await _service.UpdateAsync(jobId, dto);

            job.Title.Should().Be("New Title");
            job.Location.Should().Be("New Loc");
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenJobPostingDoesNotExist()
        {
            var dto = new JobPostingDto { Title = "Title", Description = "Desc", Requirements = "Req", Location = "Loc" };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenMinSalaryGreaterThanMaxSalary()
        {
            var jobId = Guid.NewGuid();
            _jobPostings.Add(new JobPosting { Id = jobId });

            var dto = new JobPostingDto
            {
                Title = "Title",
                Description = "Desc",
                Requirements = "Req",
                Location = "Loc",
                MinSalary = 100000,
                MaxSalary = 50000
            };

            Func<Task> act = async () => await _service.UpdateAsync(jobId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Minimum salary cannot be greater than the maximum salary*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenDeadlineIsInThePast()
        {
            var jobId = Guid.NewGuid();
            _jobPostings.Add(new JobPosting { Id = jobId });

            var dto = new JobPostingDto
            {
                Title = "Title",
                Description = "Desc",
                Requirements = "Req",
                Location = "Loc",
                Deadline = DateTime.UtcNow.AddDays(-1)
            };

            Func<Task> act = async () => await _service.UpdateAsync(jobId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Deadline must be in the future*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenDbUpdateExceptionOccurs()
        {
            var jobId = Guid.NewGuid();
            _jobPostings.Add(new JobPosting { Id = jobId });

            var dto = new JobPostingDto
            {
                Title = "Title",
                Description = "Desc",
                Requirements = "Req",
                Location = "Loc"
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.UpdateAsync(jobId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Failed to update job posting*");
        }
    }
}