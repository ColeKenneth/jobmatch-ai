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
    public sealed class StudentServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly StudentService _service;

        public StudentServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new StudentService(_context, Mock.Of<ILogger<StudentService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<User> CreateTestUserAsync(UserRole role = UserRole.Student, string? email = null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email ?? $"student_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateStudent_WhenValidDtoAndStudentUser()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var dto = new StudentDto
            {
                UserId = user.Id,
                StudentIdNumber = "23-QC12345",
                Program = "Bachelor of Science in Information Technology",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.25m,
                GraduationDate = new DateOnly(2027, 6, 15),
                ResumeFilePath = "/resumes/cole_silva.pdf",
                ResumeText = "Experienced backend developer..."
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be(user.Id);
            result.StudentIdNumber.Should().Be("23-QC12345");
            result.Program.Should().Be("Bachelor of Science in Information Technology");
            result.YearLevel.Should().Be(YearLevel.Graduating);
            result.Gwa.Should().Be(1.25m);

            var saved = await _context.Students.FindAsync(result.Id);
            saved.Should().NotBeNull();
            saved!.StudentIdNumber.Should().Be("23-QC12345");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            var dto = new StudentDto
            {
                UserId = Guid.NewGuid(),
                StudentIdNumber = "23-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*User not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenUserIsNotStudent()
        {
            var employerUser = await CreateTestUserAsync(UserRole.Employer);
            var dto = new StudentDto
            {
                UserId = employerUser.Id,
                StudentIdNumber = "23-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*User must have the student role*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenUserAlreadyHasStudentProfile()
        {
            var user = await CreateTestUserAsync(UserRole.Student);

            _context.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "22-QC11111",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.75m
            });
            await _context.SaveChangesAsync();

            var dto = new StudentDto
            {
                UserId = user.Id,
                StudentIdNumber = "23-QC22222",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*Student already exists for User*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenStudentIdNumberAlreadyExistsCaseInsensitive()
        {
            var user1 = await CreateTestUserAsync(UserRole.Student);
            var user2 = await CreateTestUserAsync(UserRole.Student);

            _context.Students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                StudentIdNumber = "23-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            });
            await _context.SaveChangesAsync();

            var dto = new StudentDto
            {
                UserId = user2.Id,
                StudentIdNumber = "23-qc12345", 
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnStudentDto_WhenStudentExists()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-QC99999",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.30m
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(student.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(student.Id);
            result.StudentIdNumber.Should().Be("23-QC99999");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenStudentDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentIdNumberAsync_ShouldReturnStudentDto_WhenStudentIdNumberExists()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-QC88888",
                Program = "BSIT",
                YearLevel = YearLevel.Alumni,
                Gwa = 1.40m
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var result = await _service.GetByStudentIdNumberAsync("  23-qc88888  ");

            result.Should().NotBeNull();
            result!.StudentIdNumber.Should().Be("23-QC88888");
            result.YearLevel.Should().Be(YearLevel.Alumni);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnStudentDto_WhenUserIdExists()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-QC77777",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.60m
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var result = await _service.GetByUserIdAsync(user.Id);

            result.Should().NotBeNull();
            result!.UserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateStudent_WhenValidDto()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-QC11111",
                Program = "Old Program",
                YearLevel = YearLevel.Graduating,
                Gwa = 2.00m
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var updateDto = new StudentDto
            {
                UserId = user.Id,
                StudentIdNumber = "23-QC11111",
                Program = "Updated Program Name",
                YearLevel = YearLevel.Alumni,
                Gwa = 1.15m
            };

            await _service.UpdateAsync(student.Id, updateDto);

            var updated = await _context.Students.FindAsync(student.Id);
            updated.Should().NotBeNull();
            updated!.Program.Should().Be("Updated Program Name");
            updated.YearLevel.Should().Be(YearLevel.Alumni);
            updated.Gwa.Should().Be(1.15m);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            var dto = new StudentDto
            {
                StudentIdNumber = "23-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Student*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenAttemptingToReassignUserId()
        {
            var user1 = await CreateTestUserAsync(UserRole.Student);
            var user2 = await CreateTestUserAsync(UserRole.Student);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                StudentIdNumber = "23-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var updateDto = new StudentDto
            {
                UserId = user2.Id, 
                StudentIdNumber = "23-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            Func<Task> act = async () => await _service.UpdateAsync(student.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Student ownership*cannot be reassigned*");
        }

        [Fact]
        public async Task UpdateResumeAsync_ShouldUpdateResumeDetails_WhenStudentExists()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-QC55555",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await _service.UpdateResumeAsync(student.Id, "  /resumes/new_path.pdf  ", "  New resume text content  ");

            _context.ChangeTracker.Clear();
            var updated = await _context.Students.FindAsync(student.Id);
            updated.Should().NotBeNull();
            updated!.ResumeFilePath.Should().Be("/resumes/new_path.pdf");
            updated.ResumeText.Should().Be("New resume text content");
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteStudent_WhenStudentExists()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-QC44444",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.75m
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(student.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.Students.FindAsync(student.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            Func<Task> act = async () => await _service.DeleteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Student*not found*");
        }
    }
}