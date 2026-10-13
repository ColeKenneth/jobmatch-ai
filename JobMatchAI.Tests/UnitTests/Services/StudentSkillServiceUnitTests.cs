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
    public class StudentSkillServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<ILogger<StudentSkillService>> _loggerMock;
        private readonly StudentSkillService _service;

        private readonly List<Student> _students = [];
        private readonly List<Skill> _skills = [];
        private readonly List<StudentSkill> _studentSkills = [];

        private readonly Guid _studentId = Guid.NewGuid();
        private readonly Guid _skillId = Guid.NewGuid();

        public StudentSkillServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);
            _loggerMock = new Mock<ILogger<StudentSkillService>>();

            _contextMock.Setup(c => c.Students).Returns(DbSetMockHelper.CreateMockDbSet(_students));
            _contextMock.Setup(c => c.Skills).Returns(DbSetMockHelper.CreateMockDbSet(_skills));
            _contextMock.Setup(c => c.StudentSkills).Returns(DbSetMockHelper.CreateMockDbSet(_studentSkills));
            _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _service = new StudentSkillService(_contextMock.Object, _loggerMock.Object);

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

            _skills.Add(new Skill
            {
                Id = _skillId,
                Name = "C#",
                Category = Domain.Enums.Category.Technical
            });
        }

        [Fact]
        public async Task AddSkillAsync_ReturnsDto_WhenValidRequest()
        {
            var dto = new StudentSkillDto
            {
                SkillId = _skillId,
                ProficiencyLevel = 4,
                YearsOfExperience = 2
            };

            var result = await _service.AddSkillAsync(_studentId, dto);

            result.Should().NotBeNull();
            result.StudentId.Should().Be(_studentId);
            result.SkillId.Should().Be(_skillId);
            result.ProficiencyLevel.Should().Be(4);
            result.YearsOfExperience.Should().Be(2);
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task AddSkillAsync_ThrowsNotFoundException_WhenStudentDoesNotExist()
        {
            var dto = new StudentSkillDto { SkillId = _skillId, ProficiencyLevel = 3 };

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.AddSkillAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task AddSkillAsync_ThrowsNotFoundException_WhenSkillDoesNotExist()
        {
            var dto = new StudentSkillDto { SkillId = Guid.NewGuid(), ProficiencyLevel = 3 };

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.AddSkillAsync(_studentId, dto));
        }

        [Fact]
        public async Task AddSkillAsync_ThrowsAlreadyExistsException_WhenSkillAlreadyAttached()
        {
            var dto = new StudentSkillDto { SkillId = _skillId, ProficiencyLevel = 3 };
            await _service.AddSkillAsync(_studentId, dto);

            await Assert.ThrowsAsync<AlreadyExistsException>(
                () => _service.AddSkillAsync(_studentId, dto));
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenExists()
        {
            await _service.AddSkillAsync(_studentId, new StudentSkillDto { SkillId = _skillId });

            var result = await _service.ExistsAsync(_studentId, _skillId);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalse_WhenDoesNotExist()
        {
            var result = await _service.ExistsAsync(_studentId, Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenExists()
        {
            var created = await _service.AddSkillAsync(_studentId, new StudentSkillDto
            {
                SkillId = _skillId,
                ProficiencyLevel = 4
            });

            var result = await _service.GetByIdAsync(created.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(created.Id);
            result.ProficiencyLevel.Should().Be(4);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentAsync_ReturnsAllSkillsForStudent()
        {
            var skill2 = new Skill
            {
                Id = Guid.NewGuid(),
                Name = "Python",
                Category = Domain.Enums.Category.Technical
            };
            _skills.Add(skill2);

            await _service.AddSkillAsync(_studentId, new StudentSkillDto { SkillId = _skillId, ProficiencyLevel = 3 });
            await _service.AddSkillAsync(_studentId, new StudentSkillDto { SkillId = skill2.Id, ProficiencyLevel = 5 });

            var result = await _service.GetByStudentAsync(_studentId);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByStudentAsync_ReturnsEmpty_WhenNoSkillsAttached()
        {
            var result = await _service.GetByStudentAsync(_studentId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateSkillAsync_UpdatesFields_WhenValidRequest()
        {
            var created = await _service.AddSkillAsync(_studentId, new StudentSkillDto
            {
                SkillId = _skillId,
                ProficiencyLevel = 2,
                YearsOfExperience = 1
            });

            var dto = new StudentSkillDto
            {
                StudentId = _studentId,
                SkillId = _skillId,
                ProficiencyLevel = 5,
                YearsOfExperience = 3
            };

            await _service.UpdateSkillAsync(created.Id, dto);

            var updated = _studentSkills.First(ss => ss.Id == created.Id);
            updated.ProficiencyLevel.Should().Be(5);
            updated.YearsOfExperience.Should().Be(3);
        }

        [Fact]
        public async Task UpdateSkillAsync_ThrowsNotFoundException_WhenDoesNotExist()
        {
            var dto = new StudentSkillDto { ProficiencyLevel = 3 };

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.UpdateSkillAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdateSkillAsync_ThrowsBusinessRuleException_WhenReassigningStudent()
        {
            var created = await _service.AddSkillAsync(_studentId, new StudentSkillDto { SkillId = _skillId });

            var dto = new StudentSkillDto
            {
                StudentId = Guid.NewGuid(),
                SkillId = _skillId,
                ProficiencyLevel = 5
            };

            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.UpdateSkillAsync(created.Id, dto));
        }

        [Fact]
        public async Task UpdateSkillAsync_ThrowsBusinessRuleException_WhenReassigningSkill()
        {
            var created = await _service.AddSkillAsync(_studentId, new StudentSkillDto { SkillId = _skillId });

            var dto = new StudentSkillDto
            {
                StudentId = _studentId,
                SkillId = Guid.NewGuid(),
                ProficiencyLevel = 5
            };

            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.UpdateSkillAsync(created.Id, dto));
        }
    }
}