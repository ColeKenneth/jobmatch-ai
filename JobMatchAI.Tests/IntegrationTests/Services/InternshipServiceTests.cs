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
    public sealed class InternshipServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly InternshipService _service;

        public InternshipServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new InternshipService(_context, Mock.Of<ILogger<InternshipService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Student> CreateTestStudentAsync()
        {
            var uniqueId = Random.Shared.Next(10000, 99999);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"student_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = UserRole.Student
            };
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = $"23-QC{uniqueId}",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.75m
            };

            _context.Users.Add(user);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        [Fact]
        public async Task CreateAsync_ShouldAddInternship_WhenStudentExistsAndValid()
        {
            var student = await CreateTestStudentAsync();

            var dto = new InternshipDto
            {
                CompanyName = "Tech Innovators",
                Position = "Software Engineering Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31),
                SkillsGained = "C#, EF Core, SQL",
                Supervisor = "Jane Doe",
                SupervisorEmail = "jane.doe@techinnovators.com"
            };

            var result = await _service.CreateAsync(student.Id, dto);

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.StudentId.Should().Be(student.Id);
            result.CompanyName.Should().Be("Tech Innovators");
            result.Position.Should().Be("Software Engineering Intern");
            result.SupervisorEmail.Should().Be("jane.doe@techinnovators.com");

            var saved = await _context.Internships.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            var dto = new InternshipDto
            {
                CompanyName = "Tech Innovators",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };

            Func<Task> act = async () => await _service.CreateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenEndDateIsBeforeStartDate()
        {
            var student = await CreateTestStudentAsync();

            var dto = new InternshipDto
            {
                CompanyName = "Tech Innovators",
                Position = "Intern",
                StartDate = new DateOnly(2026, 8, 31),
                EndDate = new DateOnly(2026, 6, 1)
            };

            Func<Task> act = async () => await _service.CreateAsync(student.Id, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*End date must be on or after the start date*");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveInternship_WhenExists()
        {
            var student = await CreateTestStudentAsync();
            var internship = new Internship
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CompanyName = "Tech Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };
            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(internship.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.Internships.FindAsync(internship.Id);
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
        public async Task ExistsAsync_ShouldReturnTrue_WhenInternshipExists()
        {
            var student = await CreateTestStudentAsync();
            var internship = new Internship
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CompanyName = "Tech Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };
            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            var result = await _service.ExistsAsync(internship.Id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenInternshipDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnInternshipDto_WhenExists()
        {
            var student = await CreateTestStudentAsync();
            var internship = new Internship
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CompanyName = "Tech Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };
            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(internship.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(internship.Id);
            result.CompanyName.Should().Be("Tech Corp");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentAsync_ShouldReturnInternships_WhenStudentExists()
        {
            var student = await CreateTestStudentAsync();
            _context.Internships.AddRange(
                new Internship { Id = Guid.NewGuid(), StudentId = student.Id, CompanyName = "Company A", Position = "Intern", StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2025, 8, 31) },
                new Internship { Id = Guid.NewGuid(), StudentId = student.Id, CompanyName = "Company B", Position = "Developer", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 8, 31) }
            );
            await _context.SaveChangesAsync();

            var results = (await _service.GetByStudentAsync(student.Id)).ToList();

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateInternship_WhenValid()
        {
            var student = await CreateTestStudentAsync();
            var internship = new Internship
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CompanyName = "Old Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };
            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            var updateDto = new InternshipDto
            {
                StudentId = student.Id,
                CompanyName = "Updated Corp",
                Position = "Junior Dev",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 9, 30),
                SkillsGained = "Updated skills"
            };

            await _service.UpdateAsync(internship.Id, updateDto);

            var updated = await _context.Internships.FindAsync(internship.Id);
            updated.Should().NotBeNull();
            updated!.CompanyName.Should().Be("Updated Corp");
            updated.Position.Should().Be("Junior Dev");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenInternshipDoesNotExist()
        {
            var updateDto = new InternshipDto
            {
                CompanyName = "Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), updateDto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenEndDateIsBeforeStartDate()
        {
            var student = await CreateTestStudentAsync();
            var internship = new Internship
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CompanyName = "Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };
            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            var updateDto = new InternshipDto
            {
                StudentId = student.Id,
                CompanyName = "Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2026, 6, 1)
            };

            Func<Task> act = async () => await _service.UpdateAsync(internship.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*End date must be on or after the start date*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenReassigningStudentId()
        {
            var student1 = await CreateTestStudentAsync();
            var student2 = await CreateTestStudentAsync();

            var internship = new Internship
            {
                Id = Guid.NewGuid(),
                StudentId = student1.Id,
                CompanyName = "Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };
            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            var updateDto = new InternshipDto
            {
                StudentId = student2.Id,
                CompanyName = "Corp",
                Position = "Intern",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 8, 31)
            };

            Func<Task> act = async () => await _service.UpdateAsync(internship.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign internship to a different student*");
        }
    }
}