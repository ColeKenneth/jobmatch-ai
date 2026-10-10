using FluentAssertions;
using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Implementations;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Enums;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using JobMatchAI.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JobMatchAI.Tests.IntegrationTests.Services
{
    public class StudentSkillServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly StudentSkillService _service;

        public StudentSkillServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new StudentSkillService(_context, Mock.Of<ILogger<StudentSkillService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Student> CreateTestStudentAsync()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "student@test.com", PasswordHash = "hash", Role = UserRole.Student };
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-IT12345",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.25m
            };

            _context.Users.Add(user);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        private async Task<Skill> CreateTestSkillAsync(string name = "C#")
        {
            var skill = new Skill
            {
                Id = Guid.NewGuid(),
                Name = name,
                Category = Category.Technical
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
            return skill;
        }

        [Fact]
        public async Task AddSkillAsync_ShouldAttachSkill_WhenStudentAndSkillExist()
        {
            var student = await CreateTestStudentAsync();
            var skill = await CreateTestSkillAsync("Java");

            var dto = new StudentSkillDto
            {
                SkillId = skill.Id,
                ProficiencyLevel = 4,
                YearsOfExperience = 2
            };

            var result = await _service.AddSkillAsync(student.Id, dto);

            result.Should().NotBeNull();
            result.StudentId.Should().Be(student.Id);
            result.SkillId.Should().Be(skill.Id);
            result.ProficiencyLevel.Should().Be(4);

            var saved = await _context.StudentSkills.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task AddSkillAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            var skill = await CreateTestSkillAsync("Python");
            var dto = new StudentSkillDto { SkillId = skill.Id, ProficiencyLevel = 3 };

            Func<Task> act = async () => await _service.AddSkillAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Student*not found*");
        }

        [Fact]
        public async Task AddSkillAsync_ShouldThrowNotFoundException_WhenSkillDoesNotExist()
        {
            var student = await CreateTestStudentAsync();
            var dto = new StudentSkillDto { SkillId = Guid.NewGuid(), ProficiencyLevel = 3 };

            Func<Task> act = async () => await _service.AddSkillAsync(student.Id, dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Skill*not found*");
        }

        [Fact]
        public async Task AddSkillAsync_ShouldThrowAlreadyExistsException_WhenSkillAlreadyAttached()
        {
            var student = await CreateTestStudentAsync();
            var skill = await CreateTestSkillAsync("Spring Boot");

            _context.StudentSkills.Add(new StudentSkill
            {
                StudentId = student.Id,
                SkillId = skill.Id,
                ProficiencyLevel = 3
            });
            await _context.SaveChangesAsync();

            var dto = new StudentSkillDto { SkillId = skill.Id, ProficiencyLevel = 5 };

            Func<Task> act = async () => await _service.AddSkillAsync(student.Id, dto);

            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task UpdateSkillAsync_ShouldUpdateProficiencyAndExperience_WhenValid()
        {
            var student = await CreateTestStudentAsync();
            var skill = await CreateTestSkillAsync("TypeScript");

            var studentSkill = new StudentSkill
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                SkillId = skill.Id,
                ProficiencyLevel = 2,
                YearsOfExperience = 1
            };
            _context.StudentSkills.Add(studentSkill);
            await _context.SaveChangesAsync();

            var updateDto = new StudentSkillDto
            {
                StudentId = student.Id,
                SkillId = skill.Id,
                ProficiencyLevel = 5,
                YearsOfExperience = 3
            };

            await _service.UpdateSkillAsync(studentSkill.Id, updateDto);

            var updated = await _context.StudentSkills.FindAsync(studentSkill.Id);
            updated.Should().NotBeNull();
            updated!.ProficiencyLevel.Should().Be(5);
            updated.YearsOfExperience.Should().Be(3);
        }

        [Fact]
        public async Task UpdateSkillAsync_ShouldThrowBusinessRuleException_WhenReassigningStudent()
        {
            var student = await CreateTestStudentAsync();
            var skill = await CreateTestSkillAsync("Docker");

            var studentSkill = new StudentSkill
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                SkillId = skill.Id,
                ProficiencyLevel = 3
            };
            _context.StudentSkills.Add(studentSkill);
            await _context.SaveChangesAsync();

            var updateDto = new StudentSkillDto
            {
                StudentId = Guid.NewGuid(), 
                SkillId = skill.Id,
                ProficiencyLevel = 4
            };

            Func<Task> act = async () => await _service.UpdateSkillAsync(studentSkill.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign student skill to a different student*");
        }

        [Fact]
        public async Task RemoveSkillAsync_ShouldDeleteStudentSkill_WhenExists()
        {
            var student = await CreateTestStudentAsync();
            var skill = await CreateTestSkillAsync("Kubernetes");

            var studentSkill = new StudentSkill
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                SkillId = skill.Id,
                ProficiencyLevel = 3
            };
            _context.StudentSkills.Add(studentSkill);
            await _context.SaveChangesAsync();

            await _service.RemoveSkillAsync(studentSkill.Id);

            _context.ChangeTracker.Clear();

            var deleted = await _context.StudentSkills.FindAsync(studentSkill.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task RemoveAllForStudentAsync_ShouldRemoveAllSkills_WhenStudentExists()
        {
            var student = await CreateTestStudentAsync();
            var skill1 = await CreateTestSkillAsync("SQL");
            var skill2 = await CreateTestSkillAsync("PostgreSQL");

            _context.StudentSkills.AddRange(
                new StudentSkill { Id = Guid.NewGuid(), StudentId = student.Id, SkillId = skill1.Id, ProficiencyLevel = 3 },
                new StudentSkill { Id = Guid.NewGuid(), StudentId = student.Id, SkillId = skill2.Id, ProficiencyLevel = 4 }
            );
            await _context.SaveChangesAsync();

            await _service.RemoveAllForStudentAsync(student.Id);

            _context.ChangeTracker.Clear();

            var remaining = await _context.StudentSkills
                .Where(ss => ss.StudentId == student.Id)
                .ToListAsync();

            remaining.Should().BeEmpty();
        }
    }
}