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
    public sealed class MatchResultServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MatchResultService _service;

        public MatchResultServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new MatchResultService(_context, Mock.Of<ILogger<MatchResultService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Employer> CreateTestEmployerAsync()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"employer_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = UserRole.Employer
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = "Tech Corp",
                Industry = "Software Development"
            };
            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();

            return employer;
        }

        private async Task<(Student student, JobPosting jobPosting)> CreateTestDataAsync()
        {
            var studentUniqueId = Random.Shared.Next(10000, 99999);

            var studentUser = new User
            {
                Id = Guid.NewGuid(),
                Email = $"student_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = UserRole.Student
            };
            _context.Users.Add(studentUser);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = studentUser.Id,
                StudentIdNumber = $"23-QC{studentUniqueId}",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.75m
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var employer = await CreateTestEmployerAsync();

            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Software Engineer",
                Description = "Develop backend software",
                EmploymentType = EmploymentType.FullTime,
                IsActive = true
            };
            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            return (student, jobPosting);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddMatchResult_WhenStudentAndJobExistAndValid()
        {
            var (student, jobPosting) = await CreateTestDataAsync();

            var dto = new MatchResultDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 95.50,
                EmployabilityScore = 88.00,
                MatchExplanation = "Strong match on skills",
                FeatureContributions = "C#, SQL"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.StudentId.Should().Be(student.Id);
            result.JobPostingId.Should().Be(jobPosting.Id);
            result.MatchScore.Should().Be(95.50);
            result.WasViewed.Should().BeFalse();
            result.WasApplied.Should().BeFalse();
            result.WasHired.Should().BeFalse();

            var saved = await _context.MatchResults.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            var (_, jobPosting) = await CreateTestDataAsync();

            var dto = new MatchResultDto
            {
                StudentId = Guid.NewGuid(),
                JobPostingId = jobPosting.Id,
                MatchScore = 85.00
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Student*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenJobPostingDoesNotExist()
        {
            var (student, _) = await CreateTestDataAsync();

            var dto = new MatchResultDto
            {
                StudentId = student.Id,
                JobPostingId = Guid.NewGuid(),
                MatchScore = 85.00
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Job posting*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenMatchResultAlreadyExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();

            var existing = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00
            };
            _context.MatchResults.Add(existing);
            await _context.SaveChangesAsync();

            var dto = new MatchResultDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 95.00
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveMatchResult_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(matchResult.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.MatchResults.FindAsync(matchResult.Id);
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
        public async Task ExistsAsync_ShouldReturnTrue_WhenMatchResultExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            var result = await _service.ExistsAsync(matchResult.Id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenMatchResultDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMatchResultDto_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(matchResult.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(matchResult.Id);
            result.MatchScore.Should().Be(90.00);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByJobPostingAsync_ShouldReturnMatchResults_WhenJobPostingExists()
        {
            var (student1, jobPosting) = await CreateTestDataAsync();

            var student2User = new User { Id = Guid.NewGuid(), Email = $"student_{Guid.NewGuid()}@test.com", PasswordHash = "hash", Role = UserRole.Student };
            _context.Users.Add(student2User);
            await _context.SaveChangesAsync();

            var student2 = new Student { Id = Guid.NewGuid(), UserId = student2User.Id, StudentIdNumber = $"23-QC{Random.Shared.Next(10000, 99999)}", Program = "BSIT", YearLevel = YearLevel.Graduating, Gwa = 1.50m };
            _context.Students.Add(student2);
            await _context.SaveChangesAsync();

            _context.MatchResults.AddRange(
                new MatchResult { Id = Guid.NewGuid(), StudentId = student1.Id, JobPostingId = jobPosting.Id, MatchScore = 80.00 },
                new MatchResult { Id = Guid.NewGuid(), StudentId = student2.Id, JobPostingId = jobPosting.Id, MatchScore = 95.00 }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetByJobPostingAsync(jobPosting.Id)).ToList();

            results.Should().HaveCount(2);
            results.First().MatchScore.Should().Be(95.00);
        }

        [Fact]
        public async Task GetByStudentAndJobAsync_ShouldReturnMatchResultDto_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            var result = await _service.GetByStudentAndJobAsync(student.Id, jobPosting.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(matchResult.Id);
        }

        [Fact]
        public async Task GetByStudentAndJobAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByStudentAndJobAsync(Guid.NewGuid(), Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentAsync_ShouldReturnMatchResults_WhenStudentExists()
        {
            var (student, jobPosting1) = await CreateTestDataAsync();
            var employer = await CreateTestEmployerAsync();

            var jobPosting2 = new JobPosting { Id = Guid.NewGuid(), EmployerId = employer.Id, Title = "Data Analyst", Description = "SQL", EmploymentType = EmploymentType.FullTime, IsActive = true };
            _context.JobPostings.Add(jobPosting2);
            await _context.SaveChangesAsync();

            _context.MatchResults.AddRange(
                new MatchResult { Id = Guid.NewGuid(), StudentId = student.Id, JobPostingId = jobPosting1.Id, MatchScore = 75.00 },
                new MatchResult { Id = Guid.NewGuid(), StudentId = student.Id, JobPostingId = jobPosting2.Id, MatchScore = 92.00 }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetByStudentAsync(student.Id)).ToList();

            results.Should().HaveCount(2);
            results.First().MatchScore.Should().Be(92.00);
        }

        [Fact]
        public async Task MarkAsAppliedAsync_ShouldUpdateStatus_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00,
                WasViewed = false,
                WasApplied = false
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            await _service.MarkAsAppliedAsync(matchResult.Id);

            _context.ChangeTracker.Clear();
            var updated = await _context.MatchResults.FindAsync(matchResult.Id);
            updated.Should().NotBeNull();
            updated!.WasViewed.Should().BeTrue();
            updated.WasApplied.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAsAppliedAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            Func<Task> act = async () => await _service.MarkAsAppliedAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task MarkAsHiredAsync_ShouldUpdateStatusAndDate_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00,
                WasViewed = false,
                WasApplied = false,
                WasHired = false
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            var hireDate = DateTime.UtcNow;
            await _service.MarkAsHiredAsync(matchResult.Id, hireDate);

            _context.ChangeTracker.Clear();
            var updated = await _context.MatchResults.FindAsync(matchResult.Id);
            updated.Should().NotBeNull();
            updated!.WasViewed.Should().BeTrue();
            updated.WasApplied.Should().BeTrue();
            updated.WasHired.Should().BeTrue();
            updated.HireDate.Should().BeCloseTo(hireDate, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task MarkAsHiredAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            Func<Task> act = async () => await _service.MarkAsHiredAsync(Guid.NewGuid(), DateTime.UtcNow);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task MarkAsViewedAsync_ShouldUpdateStatus_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00,
                WasViewed = false
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            await _service.MarkAsViewedAsync(matchResult.Id);

            _context.ChangeTracker.Clear();
            var updated = await _context.MatchResults.FindAsync(matchResult.Id);
            updated.Should().NotBeNull();
            updated!.WasViewed.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAsViewedAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            Func<Task> act = async () => await _service.MarkAsViewedAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateMatchResult_WhenValid()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 80.00,
                MatchExplanation = "Old explanation"
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            var updateDto = new MatchResultDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 95.00,
                MatchExplanation = "Updated explanation"
            };

            await _service.UpdateAsync(matchResult.Id, updateDto);

            var updated = await _context.MatchResults.FindAsync(matchResult.Id);
            updated.Should().NotBeNull();
            updated!.MatchScore.Should().Be(95.00);
            updated.MatchExplanation.Should().Be("Updated explanation");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenMatchResultDoesNotExist()
        {
            var updateDto = new MatchResultDto
            {
                MatchScore = 90.00
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), updateDto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenReassigningStudentId()
        {
            var (student1, jobPosting) = await CreateTestDataAsync();

            var student2User = new User { Id = Guid.NewGuid(), Email = $"student_{Guid.NewGuid()}@test.com", PasswordHash = "hash", Role = UserRole.Student };
            _context.Users.Add(student2User);
            await _context.SaveChangesAsync();

            var student2 = new Student { Id = Guid.NewGuid(), UserId = student2User.Id, StudentIdNumber = $"23-QC{Random.Shared.Next(10000, 99999)}", Program = "BSIT", YearLevel = YearLevel.Graduating, Gwa = 1.75m };
            _context.Students.Add(student2);
            await _context.SaveChangesAsync();

            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student1.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            var updateDto = new MatchResultDto
            {
                StudentId = student2.Id,
                JobPostingId = jobPosting.Id,
                MatchScore = 90.00
            };

            Func<Task> act = async () => await _service.UpdateAsync(matchResult.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign match result to a different student*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenReassigningJobPostingId()
        {
            var (student, jobPosting1) = await CreateTestDataAsync();
            var employer = await CreateTestEmployerAsync();

            var jobPosting2 = new JobPosting { Id = Guid.NewGuid(), EmployerId = employer.Id, Title = "Manager", Description = "Lead", EmploymentType = EmploymentType.FullTime, IsActive = true };
            _context.JobPostings.Add(jobPosting2);
            await _context.SaveChangesAsync();

            var matchResult = new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting1.Id,
                MatchScore = 90.00
            };
            _context.MatchResults.Add(matchResult);
            await _context.SaveChangesAsync();

            var updateDto = new MatchResultDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting2.Id,
                MatchScore = 90.00
            };

            Func<Task> act = async () => await _service.UpdateAsync(matchResult.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign match result to a different job posting*");
        }
    }
}