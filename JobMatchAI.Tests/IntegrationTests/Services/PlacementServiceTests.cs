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
    public class PlacementServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly PlacementService _service;

        public PlacementServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new PlacementService(_context, Mock.Of<ILogger<PlacementService>>());
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
                StudentIdNumber = $"23-IT{studentUniqueId}",
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
                Description = "Develop enterprise backend software",
                EmploymentType = EmploymentType.FullTime,
                IsActive = true
            };
            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();

            return (student, jobPosting);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddPlacementRecord_WhenStudentAndJobExistAndValid()
        {
            var (student, jobPosting) = await CreateTestDataAsync();

            var dto = new PlacementRecordDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.OfferAccepted,
                Notes = "Excited to join the team."
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.StudentId.Should().Be(student.Id);
            result.JobPostingId.Should().Be(jobPosting.Id);
            result.PlacementStatus.Should().Be(PlacementStatus.OfferAccepted);
            result.Notes.Should().Be("Excited to join the team.");

            var saved = await _context.PlacementRecords.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            var (_, jobPosting) = await CreateTestDataAsync();

            var dto = new PlacementRecordDto
            {
                StudentId = Guid.NewGuid(),
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Student*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenJobPostingDoesNotExist()
        {
            var (student, _) = await CreateTestDataAsync();

            var dto = new PlacementRecordDto
            {
                StudentId = student.Id,
                JobPostingId = Guid.NewGuid(),
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Job posting*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenPlacementRecordAlreadyExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();

            var existing = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };
            _context.PlacementRecords.Add(existing);
            await _context.SaveChangesAsync();

            var dto = new PlacementRecordDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Hired
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemovePlacementRecord_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var record = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };
            _context.PlacementRecords.Add(record);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(record.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.PlacementRecords.FindAsync(record.Id);
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
        public async Task ExistsAsync_ShouldReturnTrue_WhenPlacementRecordExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var record = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };
            _context.PlacementRecords.Add(record);
            await _context.SaveChangesAsync();

            var result = await _service.ExistsAsync(record.Id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenPlacementRecordDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnPlacementRecordDto_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var record = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Hired
            };
            _context.PlacementRecords.Add(record);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(record.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(record.Id);
            result.PlacementStatus.Should().Be(PlacementStatus.Hired);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByJobPostingAsync_ShouldReturnPlacementRecords_WhenJobPostingExists()
        {
            var (student1, jobPosting) = await CreateTestDataAsync();

            var student2User = new User { Id = Guid.NewGuid(), Email = $"student_{Guid.NewGuid()}@test.com", PasswordHash = "hash", Role = UserRole.Student };
            _context.Users.Add(student2User);
            await _context.SaveChangesAsync();

            var student2 = new Student { Id = Guid.NewGuid(), UserId = student2User.Id, StudentIdNumber = $"23-IT{Random.Shared.Next(10000, 99999)}", Program = "BSIT", YearLevel = YearLevel.Graduating, Gwa = 1.50m };
            _context.Students.Add(student2);
            await _context.SaveChangesAsync();

            _context.PlacementRecords.AddRange(
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = student1.Id, JobPostingId = jobPosting.Id, PlacementDate = DateTime.UtcNow, PlacementStatus = PlacementStatus.Pending },
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = student2.Id, JobPostingId = jobPosting.Id, PlacementDate = DateTime.UtcNow, PlacementStatus = PlacementStatus.Hired }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetByJobPostingAsync(jobPosting.Id)).ToList();

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByStatusAsync_ShouldReturnOrderedPlacementRecords_WhenStatusExists()
        {
            var (student1, jobPosting1) = await CreateTestDataAsync();
            var employer = await CreateTestEmployerAsync();

            var jobPosting2 = new JobPosting { Id = Guid.NewGuid(), EmployerId = employer.Id, Title = "QA Engineer", Description = "Testing", EmploymentType = EmploymentType.FullTime, IsActive = true };
            _context.JobPostings.Add(jobPosting2);
            await _context.SaveChangesAsync();

            var olderDate = DateTime.UtcNow.AddDays(-5);
            var newerDate = DateTime.UtcNow;

            _context.PlacementRecords.AddRange(
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = student1.Id, JobPostingId = jobPosting1.Id, PlacementDate = olderDate, PlacementStatus = PlacementStatus.OfferAccepted },
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = student1.Id, JobPostingId = jobPosting2.Id, PlacementDate = newerDate, PlacementStatus = PlacementStatus.OfferAccepted }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetByStatusAsync(PlacementStatus.OfferAccepted)).ToList();

            results.Should().HaveCount(2);
            results.First().PlacementDate.Should().BeCloseTo(newerDate, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetByStudentAsync_ShouldReturnPlacementRecords_WhenStudentExists()
        {
            var (student, jobPosting1) = await CreateTestDataAsync();
            var employer = await CreateTestEmployerAsync();

            var jobPosting2 = new JobPosting { Id = Guid.NewGuid(), EmployerId = employer.Id, Title = "Data Analyst", Description = "SQL", EmploymentType = EmploymentType.FullTime, IsActive = true };
            _context.JobPostings.Add(jobPosting2);
            await _context.SaveChangesAsync();

            _context.PlacementRecords.AddRange(
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = student.Id, JobPostingId = jobPosting1.Id, PlacementDate = DateTime.UtcNow, PlacementStatus = PlacementStatus.Pending },
                new PlacementRecord { Id = Guid.NewGuid(), StudentId = student.Id, JobPostingId = jobPosting2.Id, PlacementDate = DateTime.UtcNow, PlacementStatus = PlacementStatus.Internship }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetByStudentAsync(student.Id)).ToList();

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdatePlacementRecord_WhenValid()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var record = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow.AddDays(-1),
                PlacementStatus = PlacementStatus.Pending,
                Notes = "Old notes"
            };
            _context.PlacementRecords.Add(record);
            await _context.SaveChangesAsync();

            var updateDto = new PlacementRecordDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Hired,
                Notes = "Updated notes"
            };

            await _service.UpdateAsync(record.Id, updateDto);

            var updated = await _context.PlacementRecords.FindAsync(record.Id);
            updated.Should().NotBeNull();
            updated!.PlacementStatus.Should().Be(PlacementStatus.Hired);
            updated.Notes.Should().Be("Updated notes");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenPlacementRecordDoesNotExist()
        {
            var updateDto = new PlacementRecordDto
            {
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Hired
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

            var student2 = new Student { Id = Guid.NewGuid(), UserId = student2User.Id, StudentIdNumber = $"23-IT{Random.Shared.Next(10000, 99999)}", Program = "BSIT", YearLevel = YearLevel.Graduating, Gwa = 1.75m };
            _context.Students.Add(student2);
            await _context.SaveChangesAsync();

            var record = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student1.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };
            _context.PlacementRecords.Add(record);
            await _context.SaveChangesAsync();

            var updateDto = new PlacementRecordDto
            {
                StudentId = student2.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };

            Func<Task> act = async () => await _service.UpdateAsync(record.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*reassign*student*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenReassigningJobPostingId()
        {
            var (student, jobPosting1) = await CreateTestDataAsync();
            var employer = await CreateTestEmployerAsync();

            var jobPosting2 = new JobPosting { Id = Guid.NewGuid(), EmployerId = employer.Id, Title = "Manager", Description = "Lead", EmploymentType = EmploymentType.FullTime, IsActive = true };
            _context.JobPostings.Add(jobPosting2);
            await _context.SaveChangesAsync();

            var record = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting1.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };
            _context.PlacementRecords.Add(record);
            await _context.SaveChangesAsync();

            var updateDto = new PlacementRecordDto
            {
                StudentId = student.Id,
                JobPostingId = jobPosting2.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };

            Func<Task> act = async () => await _service.UpdateAsync(record.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*reassign*job posting*");
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldUpdateStatus_WhenExists()
        {
            var (student, jobPosting) = await CreateTestDataAsync();
            var record = new PlacementRecord
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                JobPostingId = jobPosting.Id,
                PlacementDate = DateTime.UtcNow,
                PlacementStatus = PlacementStatus.Pending
            };
            _context.PlacementRecords.Add(record);
            await _context.SaveChangesAsync();

            await _service.UpdateStatusAsync(record.Id, PlacementStatus.OfferAccepted);

            _context.ChangeTracker.Clear();
            var updated = await _context.PlacementRecords.FindAsync(record.Id);
            updated.Should().NotBeNull();
            updated!.PlacementStatus.Should().Be(PlacementStatus.OfferAccepted);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            Func<Task> act = async () => await _service.UpdateStatusAsync(Guid.NewGuid(), PlacementStatus.Hired);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }
    }
}