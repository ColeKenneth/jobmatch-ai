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
    public class PlacementServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<ILogger<PlacementService>> _loggerMock;
        private readonly PlacementService _service;

        private readonly List<Student> _students = [];
        private readonly List<JobPosting> _jobPostings = [];
        private readonly List<PlacementRecord> _placementRecords = [];

        private readonly Guid _studentId = Guid.NewGuid();
        private readonly Guid _jobPostingId = Guid.NewGuid();

        public PlacementServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);
            _loggerMock = new Mock<ILogger<PlacementService>>();

            _contextMock.Setup(c => c.Students).Returns(DbSetMockHelper.CreateMockDbSet(_students));
            _contextMock.Setup(c => c.JobPostings).Returns(DbSetMockHelper.CreateMockDbSet(_jobPostings));
            _contextMock.Setup(c => c.PlacementRecords).Returns(DbSetMockHelper.CreateMockDbSet(_placementRecords));

            _service = new PlacementService(_contextMock.Object, _loggerMock.Object);

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
                EmploymentType = EmploymentType.FullTime
            });
        }

        [Fact]
        public async Task CreateAsync_ReturnsDto_WhenValidRequest()
        {
            var dto = new PlacementRecordDto
            {
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending,
                Notes = "Initial placement"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.StudentId.Should().Be(_studentId);
            result.JobPostingId.Should().Be(_jobPostingId);
            result.PlacementStatus.Should().Be(PlacementStatus.Pending);
            result.Notes.Should().Be("Initial placement");
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateAsync_ThrowsNotFoundException_WhenStudentDoesNotExist()
        {
            var dto = new PlacementRecordDto
            {
                StudentId = Guid.NewGuid(),
                JobPostingId = _jobPostingId,
                PlacementStatus = PlacementStatus.Pending
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsNotFoundException_WhenJobPostingDoesNotExist()
        {
            var dto = new PlacementRecordDto
            {
                StudentId = _studentId,
                JobPostingId = Guid.NewGuid(),
                PlacementStatus = PlacementStatus.Pending
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenDuplicatePair()
        {
            _placementRecords.Add(new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                PlacementStatus = PlacementStatus.Pending
            });

            var dto = new PlacementRecordDto
            {
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                PlacementStatus = PlacementStatus.Hired
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenExists()
        {
            var recordId = Guid.NewGuid();
            _placementRecords.Add(new PlacementRecord { Id = recordId });

            var result = await _service.ExistsAsync(recordId);

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
            var recordId = Guid.NewGuid();
            _placementRecords.Add(new PlacementRecord
            {
                Id = recordId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                PlacementStatus = PlacementStatus.Hired,
                PlacementDate = DateTime.UtcNow
            });

            var result = await _service.GetByIdAsync(recordId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(recordId);
            result.PlacementStatus.Should().Be(PlacementStatus.Hired);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentAsync_ReturnsAllRecordsForStudent()
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
                EmploymentType = EmploymentType.FullTime
            });

            _placementRecords.AddRange(
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = _studentId, JobPostingId = _jobPostingId },
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = _studentId, JobPostingId = otherJobId },
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), JobPostingId = _jobPostingId }
            );

            var result = await _service.GetByStudentAsync(_studentId);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByStudentAsync_ReturnsEmpty_WhenNoRecords()
        {
            var result = await _service.GetByStudentAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByJobPostingAsync_ReturnsAllRecordsForJob()
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

            _placementRecords.AddRange(
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = _studentId, JobPostingId = _jobPostingId },
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = otherStudentId, JobPostingId = _jobPostingId }
            );

            var result = await _service.GetByJobPostingAsync(_jobPostingId);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByJobPostingAsync_ReturnsEmpty_WhenNoRecords()
        {
            var result = await _service.GetByJobPostingAsync(Guid.NewGuid());

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByStatusAsync_ReturnsFilteredRecords()
        {
            _placementRecords.AddRange(
                new PlacementRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = _studentId,
                    JobPostingId = _jobPostingId,
                    PlacementStatus = PlacementStatus.Hired,
                    PlacementDate = DateTime.UtcNow.AddDays(-1)
                },
                new PlacementRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    JobPostingId = Guid.NewGuid(),
                    PlacementStatus = PlacementStatus.Rejected,
                    PlacementDate = DateTime.UtcNow
                },
                new PlacementRecord
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    JobPostingId = Guid.NewGuid(),
                    PlacementStatus = PlacementStatus.Hired,
                    PlacementDate = DateTime.UtcNow
                }
            );

            var result = await _service.GetByStatusAsync(PlacementStatus.Hired);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByStatusAsync_ReturnsEmpty_WhenNoMatches()
        {
            var result = await _service.GetByStatusAsync(PlacementStatus.Internship);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields_WhenValidRequest()
        {
            var recordId = Guid.NewGuid();
            _placementRecords.Add(new PlacementRecord
            {
                Id = recordId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                PlacementStatus = PlacementStatus.Pending,
                Notes = "Old"
            });

            var dto = new PlacementRecordDto
            {
                StudentId = _studentId,
                JobPostingId = _jobPostingId,
                PlacementStatus = PlacementStatus.Hired,
                PlacementDate = DateTime.UtcNow,
                Notes = "Updated notes"
            };

            await _service.UpdateAsync(recordId, dto);

            var updated = _placementRecords.First(pr => pr.Id == recordId);
            updated.PlacementStatus.Should().Be(PlacementStatus.Hired);
            updated.Notes.Should().Be("Updated notes");
        }

        [Fact]
        public async Task UpdateAsync_ThrowsNotFoundException_WhenDoesNotExist()
        {
            var dto = new PlacementRecordDto { PlacementStatus = PlacementStatus.Hired };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsBusinessRuleException_WhenReassigningStudent()
        {
            var recordId = Guid.NewGuid();
            _placementRecords.Add(new PlacementRecord
            {
                Id = recordId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId
            });

            var dto = new PlacementRecordDto
            {
                StudentId = Guid.NewGuid(),
                PlacementStatus = PlacementStatus.Hired
            };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(recordId, dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsBusinessRuleException_WhenReassigningJobPosting()
        {
            var recordId = Guid.NewGuid();
            _placementRecords.Add(new PlacementRecord
            {
                Id = recordId,
                StudentId = _studentId,
                JobPostingId = _jobPostingId
            });

            var dto = new PlacementRecordDto
            {
                JobPostingId = Guid.NewGuid(),
                PlacementStatus = PlacementStatus.Hired
            };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(recordId, dto));
        }
    }
}