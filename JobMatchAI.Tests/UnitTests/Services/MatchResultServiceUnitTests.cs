using FluentAssertions;
using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Implementations;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using JobMatchAI.Tests.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobMatchAI.Tests.UnitTests.Services
{
    public class MatchResultServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<ILogger<MatchResultService>> _loggerMock;
        private readonly MatchResultService _service;

        private readonly List<Student> _students = [];
        private readonly List<JobPosting> _jobPostings = [];
        private readonly List<MatchResult> _matchResults = [];

        private readonly Guid _studentId = Guid.NewGuid();
        private readonly Guid _jobPostingId = Guid.NewGuid();

        public MatchResultServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);
            _loggerMock = new Mock<ILogger<MatchResultService>>();

            _contextMock.Setup(c => c.Students).Returns(DbSetMockHelper.CreateMockDbSet(_students));
            _contextMock.Setup(c => c.JobPostings).Returns(DbSetMockHelper.CreateMockDbSet(_jobPostings));
            _contextMock.Setup(c => c.MatchResults).Returns(DbSetMockHelper.CreateMockDbSet(_matchResults));

            _service = new MatchResultService(_contextMock.Object, _loggerMock.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _students.Add(new Student
            {
                Id = _studentId,
                UserId = Guid.NewGuid(),
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                Gwa = 1.5m
            });

            _jobPostings.Add(new JobPosting
            {
                Id = _jobPostingId,
                EmployerId = Guid.NewGuid(),
                Title = "Software Engineer",
                Description = "Test",
                Requirements = "Test",
                Location = "Manila",
                EmploymentType = Domain.Enums.EmploymentType.FullTime
            });
        }

        [Fact]
        public async Task CreateAsync_ReturnsDto_WhenValidRequest()
        {
            var dto = new MatchResultDto
            {
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 85.5,
                EmployabilityScore = 78.0,
                MatchExplanation = "Strong match",
                FeatureContributions = "{\"gpa\":0.3}"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.StudentId.Should().Be(_studentId);
            result.JobPostingId.Should().Be(_jobPostingId);
            result.MatchScore.Should().Be(85.5);
            result.EmployabilityScore.Should().Be(78.0);
            result.MatchExplanation.Should().Be("Strong match");
            result.WasViewed.Should().BeFalse();
            result.WasApplied.Should().BeFalse();
            result.WasHired.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_ThrowsNotFoundException_WhenStudentDoesNotExist()
        {
            var dto = new MatchResultDto
            {
                StudentId = Guid.NewGuid(),
                JobPostingId = _jobPostingId,
                MatchScore = 85.5
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsNotFoundException_WhenJobPostingDoesNotExist()
        {
            var dto = new MatchResultDto
            {
                StudentId = _studentId,
                JobPostingId = Guid.NewGuid(),
                MatchScore = 85.5
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenDuplicatePair()
        {
            _matchResults.Add(new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 70
            });

            var dto = new MatchResultDto
            {
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 85.5
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenExists()
        {
            var matchId = Guid.NewGuid();
            _matchResults.Add(new MatchResult { Id = matchId, MatchScore = 80 });

            var result = await _service.ExistsAsync(matchId);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalse_WhenDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenExists()
        {
            var matchId = Guid.NewGuid();
            _matchResults.Add(new MatchResult
            {
                Id = matchId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 92.5
            });

            var result = await _service.GetByIdAsync(matchId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(matchId);
            result.MatchScore.Should().Be(92.5);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentAsync_ReturnsAllMatchesForStudent()
        {
            var otherJobId = Guid.NewGuid();
            _jobPostings.Add(new JobPosting
            {
                Id = otherJobId,
                EmployerId = Guid.NewGuid(),
                Title = "Data Analyst",
                Description = "Test",
                Requirements = "Test",
                Location = "Manila",
                EmploymentType = Domain.Enums.EmploymentType.FullTime
            });

            _matchResults.AddRange(
                new MatchResult { Id = Guid.NewGuid(), StudentId = _studentId, JobPostingId = _jobPostingId, MatchScore = 70 },
                new MatchResult { Id = Guid.NewGuid(), StudentId = _studentId, JobPostingId = otherJobId, MatchScore = 95 },
                new MatchResult { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), JobPostingId = _jobPostingId, MatchScore = 60 }
            );

            var result = (await _service.GetByStudentAsync(_studentId)).ToList();

            result.Should().HaveCount(2);
            result.First().MatchScore.Should().Be(95);
        }

        [Fact]
        public async Task GetByStudentAsync_ReturnsEmpty_WhenNoMatches()
        {
            var result = await _service.GetByStudentAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByJobPostingAsync_ReturnsAllMatchesForJob()
        {
            var otherStudentId = Guid.NewGuid();
            _students.Add(new Student
            {
                Id = otherStudentId,
                UserId = Guid.NewGuid(),
                StudentIdNumber = "23-QC67890",
                Program = "BSCS",
                Gwa = 1.3m
            });

            _matchResults.AddRange(
                new MatchResult { Id = Guid.NewGuid(), StudentId = _studentId, JobPostingId = _jobPostingId, MatchScore = 70 },
                new MatchResult { Id = Guid.NewGuid(), StudentId = otherStudentId, JobPostingId = _jobPostingId, MatchScore = 95 }
            );

            var result = (await _service.GetByJobPostingAsync(_jobPostingId)).ToList();

            result.Should().HaveCount(2);
            result.First().MatchScore.Should().Be(95);
        }

        [Fact]
        public async Task GetByJobPostingAsync_ReturnsEmpty_WhenNoMatches()
        {
            var result = await _service.GetByJobPostingAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByStudentAndJobAsync_ReturnsMatch_WhenExists()
        {
            _matchResults.Add(new MatchResult
            {
                Id = Guid.NewGuid(),
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 88
            });

            var result = await _service.GetByStudentAndJobAsync(_studentId, _jobPostingId);

            result.Should().NotBeNull();
            result!.MatchScore.Should().Be(88);
        }

        [Fact]
        public async Task GetByStudentAndJobAsync_ReturnsNull_WhenNoMatch()
        {
            var result = await _service.GetByStudentAndJobAsync(Guid.NewGuid(), Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields_WhenValidRequest()
        {
            var matchId = Guid.NewGuid();
            _matchResults.Add(new MatchResult
            {
                Id = matchId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 70,
                WasViewed = false
            });

            var dto = new MatchResultDto
            {
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 90,
                EmployabilityScore = 85,
                MatchExplanation = "Updated",
                WasViewed = true,
                WasApplied = true
            };

            await _service.UpdateAsync(matchId, dto);

            var updated = _matchResults.First(mr => mr.Id == matchId);
            updated.MatchScore.Should().Be(90);
            updated.EmployabilityScore.Should().Be(85);
            updated.MatchExplanation.Should().Be("Updated");
            updated.WasViewed.Should().BeTrue();
            updated.WasApplied.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_ThrowsNotFoundException_WhenDoesNotExist()
        {
            var dto = new MatchResultDto { MatchScore = 90 };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsBusinessRuleException_WhenReassigningStudent()
        {
            var matchId = Guid.NewGuid();
            _matchResults.Add(new MatchResult
            {
                Id = matchId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 70
            });

            var dto = new MatchResultDto
            {
                StudentId = Guid.NewGuid(),
                MatchScore = 90
            };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(matchId, dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsBusinessRuleException_WhenReassigningJobPosting()
        {
            var matchId = Guid.NewGuid();
            _matchResults.Add(new MatchResult
            {
                Id = matchId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                MatchScore = 70
            });

            var dto = new MatchResultDto
            {
                JobPostingId = Guid.NewGuid(),
                MatchScore = 90
            };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(matchId, dto));
        }
    }
}