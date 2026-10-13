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
    public class StudentServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<ILogger<StudentService>> _loggerMock;
        private readonly StudentService _service;

        private readonly List<User> _users = [];
        private readonly List<Student> _students = [];

        private readonly Guid _userId = Guid.NewGuid();

        public StudentServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);
            _loggerMock = new Mock<ILogger<StudentService>>();

            _contextMock.Setup(c => c.Users).Returns(DbSetMockHelper.CreateMockDbSet(_users));
            _contextMock.Setup(c => c.Students).Returns(DbSetMockHelper.CreateMockDbSet(_students));

            _service = new StudentService(_contextMock.Object, _loggerMock.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _users.Add(new User
            {
                Id = _userId,
                FirstName = "Cole",
                LastName = "Silva",
                Email = "cole.silva@test.com",
                PasswordHash = "hash",
                Role = UserRole.Student,
                IsActive = true
            });
        }

        [Fact]
        public async Task CreateAsync_ReturnsDto_WhenValidRequest()
        {
            var dto = new StudentDto
            {
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m,
                GraduationDate = new DateOnly(2026, 6, 15)
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be(_userId);
            result.StudentIdNumber.Should().Be("24-QC12345");
            result.Program.Should().Be("BSIT");
            result.YearLevel.Should().Be(YearLevel.Graduating);
            result.Gwa.Should().Be(1.50m);
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateAsync_NormalizesStudentIdNumber_ToUpperCase()
        {
            var dto = new StudentDto
            {
                UserId = _userId,
                StudentIdNumber = "24-qc12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            var result = await _service.CreateAsync(dto);

            result.StudentIdNumber.Should().Be("24-QC12345");
        }

        [Fact]
        public async Task CreateAsync_ThrowsNotFoundException_WhenUserDoesNotExist()
        {
            var dto = new StudentDto
            {
                UserId = Guid.NewGuid(),
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsBusinessRuleException_WhenUserIsNotStudent()
        {
            var employerUserId = Guid.NewGuid();
            _users.Add(new User
            {
                Id = employerUserId,
                FirstName = "Test",
                LastName = "Employer",
                Email = "employer@test.com",
                PasswordHash = "hash",
                Role = UserRole.Employer,
                IsActive = true
            });

            var dto = new StudentDto
            {
                UserId = employerUserId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenUserAlreadyHasProfile()
        {
            _students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                StudentIdNumber = "23-QC11111",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            });

            var dto = new StudentDto
            {
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenStudentIdNumberExists()
        {
            _students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            });

            var dto = new StudentDto
            {
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSCS",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.30m
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenExists()
        {
            var studentId = Guid.NewGuid();
            _students.Add(new Student { Id = studentId, UserId = _userId });

            var result = await _service.ExistsAsync(studentId);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalse_WhenDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllStudents()
        {
            _students.AddRange(
                new Student { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), StudentIdNumber = "24-QC12345" },
                new Student { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), StudentIdNumber = "23-QC67890" }
            );

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmpty_WhenNoStudents()
        {
            var result = await _service.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenExists()
        {
            var studentId = Guid.NewGuid();
            _students.Add(new Student
            {
                Id = studentId,
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                Gwa = 1.50m
            });

            var result = await _service.GetByIdAsync(studentId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(studentId);
            result.StudentIdNumber.Should().Be("24-QC12345");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentIdNumberAsync_ReturnsDto_WhenExists()
        {
            _students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                Gwa = 1.50m
            });

            var result = await _service.GetByStudentIdNumberAsync("24-QC12345");

            result.Should().NotBeNull();
            result!.StudentIdNumber.Should().Be("24-QC12345");
        }

        [Fact]
        public async Task GetByStudentIdNumberAsync_IsCaseInsensitive()
        {
            _students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                Gwa = 1.50m
            });

            var result = await _service.GetByStudentIdNumberAsync("24-qc12345");

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByStudentIdNumberAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByStudentIdNumberAsync("99-QC00000");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsDto_WhenExists()
        {
            _students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                Gwa = 1.50m
            });

            var result = await _service.GetByUserIdAsync(_userId);

            result.Should().NotBeNull();
            result!.UserId.Should().Be(_userId);
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByUserIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task StudentIdNumberExistsAsync_ReturnsTrue_WhenExists()
        {
            _students.Add(new Student
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                StudentIdNumber = "24-QC12345"
            });

            var result = await _service.StudentIdNumberExistsAsync("24-QC12345");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task StudentIdNumberExistsAsync_ReturnsFalse_WhenDoesNotExist()
        {
            var result = await _service.StudentIdNumberExistsAsync("99-QC00000");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields_WhenValidRequest()
        {
            var studentId = Guid.NewGuid();
            _students.Add(new Student
            {
                Id = studentId,
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            });

            var dto = new StudentDto
            {
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSCS",
                YearLevel = YearLevel.Alumni,
                Gwa = 1.30m
            };

            await _service.UpdateAsync(studentId, dto);

            var updated = _students.First(s => s.Id == studentId);
            updated.Program.Should().Be("BSCS");
            updated.YearLevel.Should().Be(YearLevel.Alumni);
            updated.Gwa.Should().Be(1.30m);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsAlreadyExistsException_WhenStudentIdNumberConflicts()
        {
            var studentId = Guid.NewGuid();
            _students.AddRange(
                new Student { Id = studentId, UserId = _userId, StudentIdNumber = "24-QC12345" },
                new Student { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), StudentIdNumber = "23-QC67890" }
            );

            var dto = new StudentDto
            {
                UserId = _userId,
                StudentIdNumber = "23-QC67890",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.UpdateAsync(studentId, dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsNotFoundException_WhenDoesNotExist()
        {
            var dto = new StudentDto
            {
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsBusinessRuleException_WhenReassigningUser()
        {
            var studentId = Guid.NewGuid();
            _students.Add(new Student
            {
                Id = studentId,
                UserId = _userId,
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            });

            var dto = new StudentDto
            {
                UserId = Guid.NewGuid(),
                StudentIdNumber = "24-QC12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(studentId, dto));
        }
    }
}