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
    public class JobSkillServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly JobSkillService _service;

        public JobSkillServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new JobSkillService(_context, Mock.Of<ILogger<JobSkillService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<(JobPosting JobPosting, Skill Skill)> CreateTestPrerequisitesAsync()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"employer_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = UserRole.Employer
            };

            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = $"Tech Corp {Guid.NewGuid().ToString()[..6]}",
                Industry = "Software"
            };

            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                EmployerId = employer.Id,
                Title = "Software Engineer",
                Description = "Backend development",
                Requirements = "C#",
                Location = "Quezon City",
                EmploymentType = EmploymentType.FullTime
            };

            var skill = new Skill
            {
                Id = Guid.NewGuid(),
                Name = $"C# {Guid.NewGuid().ToString()[..6]}",
                Category = Category.Technical
            };

            _context.Users.Add(user);
            _context.Employers.Add(employer);
            _context.JobPostings.Add(jobPosting);
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return (jobPosting, skill);
        }

        [Fact]
        public async Task AddSkillAsync_ShouldAddJobSkill_WhenJobAndSkillExist()
        {
            var (job, skill) = await CreateTestPrerequisitesAsync();
            var dto = new JobSkillDto
            {
                SkillId = skill.Id,
                IsRequired = true,
                MinimumProficiencyLevel = 4,
                Priority = 5
            };

            var result = await _service.AddSkillAsync(job.Id, dto);

            result.Should().NotBeNull();
            result.JobPostingId.Should().Be(job.Id);
            result.SkillId.Should().Be(skill.Id);
            result.MinimumProficiencyLevel.Should().Be(4);

            var saved = await _context.JobSkills.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task AddSkillAsync_ShouldThrowNotFoundException_WhenJobPostingDoesNotExist()
        {
            var (_, skill) = await CreateTestPrerequisitesAsync();
            var dto = new JobSkillDto { SkillId = skill.Id };

            Func<Task> act = async () => await _service.AddSkillAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Job posting*not found*");
        }

        [Fact]
        public async Task AddSkillAsync_ShouldThrowNotFoundException_WhenSkillDoesNotExist()
        {
            var (job, _) = await CreateTestPrerequisitesAsync();
            var dto = new JobSkillDto { SkillId = Guid.NewGuid() };

            Func<Task> act = async () => await _service.AddSkillAsync(job.Id, dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Skill*not found*");
        }

        [Fact]
        public async Task AddSkillAsync_ShouldThrowAlreadyExistsException_WhenSkillAlreadyAttached()
        {
            var (job, skill) = await CreateTestPrerequisitesAsync();
            _context.JobSkills.Add(new JobSkill
            {
                JobPostingId = job.Id,
                SkillId = skill.Id,
                IsRequired = true
            });
            await _context.SaveChangesAsync();

            var dto = new JobSkillDto { SkillId = skill.Id };

            Func<Task> act = async () => await _service.AddSkillAsync(job.Id, dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*Skill is already attached*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnJobSkillDto_WhenExists()
        {
            var (job, skill) = await CreateTestPrerequisitesAsync();
            var jobSkill = new JobSkill
            {
                Id = Guid.NewGuid(),
                JobPostingId = job.Id,
                SkillId = skill.Id,
                IsRequired = false,
                MinimumProficiencyLevel = 3
            };
            _context.JobSkills.Add(jobSkill);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(jobSkill.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(jobSkill.Id);
            result.MinimumProficiencyLevel.Should().Be(3);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByJobPostingAsync_ShouldReturnJobSkillsOrderedByPriority_WhenJobExists()
        {
            var (job, skill1) = await CreateTestPrerequisitesAsync();
            var skill2 = new Skill { Id = Guid.NewGuid(), Name = "Java", Category = Category.Technical };
            _context.Skills.Add(skill2);

            _context.JobSkills.AddRange(
                new JobSkill { Id = Guid.NewGuid(), JobPostingId = job.Id, SkillId = skill1.Id, Priority = 2 },
                new JobSkill { Id = Guid.NewGuid(), JobPostingId = job.Id, SkillId = skill2.Id, Priority = 5 }
            );
            await _context.SaveChangesAsync();

            var results = await _service.GetByJobPostingAsync(job.Id);

            results.Should().HaveCount(2);
            results.First().Priority.Should().Be(5);
        }

        [Fact]
        public async Task UpdateSkillAsync_ShouldUpdateJobSkill_WhenValid()
        {
            var (job, skill) = await CreateTestPrerequisitesAsync();
            var jobSkill = new JobSkill
            {
                Id = Guid.NewGuid(),
                JobPostingId = job.Id,
                SkillId = skill.Id,
                IsRequired = true,
                MinimumProficiencyLevel = 2,
                Priority = 1
            };
            _context.JobSkills.Add(jobSkill);
            await _context.SaveChangesAsync();

            var updateDto = new JobSkillDto
            {
                JobPostingId = job.Id,
                SkillId = skill.Id,
                IsRequired = false,
                MinimumProficiencyLevel = 5,
                Priority = 4
            };

            await _service.UpdateSkillAsync(jobSkill.Id, updateDto);

            var updated = await _context.JobSkills.FindAsync(jobSkill.Id);
            updated.Should().NotBeNull();
            updated!.IsRequired.Should().BeFalse();
            updated.MinimumProficiencyLevel.Should().Be(5);
            updated.Priority.Should().Be(4);
        }

        [Fact]
        public async Task UpdateSkillAsync_ShouldThrowNotFoundException_WhenJobSkillDoesNotExist()
        {
            var dto = new JobSkillDto { SkillId = Guid.NewGuid(), IsRequired = true };

            Func<Task> act = async () => await _service.UpdateSkillAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Job skill*not found*");
        }

        [Fact]
        public async Task RemoveSkillAsync_ShouldDeleteJobSkill_WhenExists()
        {
            var (job, skill) = await CreateTestPrerequisitesAsync();
            var jobSkill = new JobSkill
            {
                Id = Guid.NewGuid(),
                JobPostingId = job.Id,
                SkillId = skill.Id
            };
            _context.JobSkills.Add(jobSkill);
            await _context.SaveChangesAsync();

            await _service.RemoveSkillAsync(jobSkill.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.JobSkills.FindAsync(jobSkill.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task RemoveSkillAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            Func<Task> act = async () => await _service.RemoveSkillAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Job skill*not found*");
        }

        [Fact]
        public async Task RemoveAllForJobPostingAsync_ShouldRemoveAllSkills_WhenJobExists()
        {
            var (job, skill1) = await CreateTestPrerequisitesAsync();
            var skill2 = new Skill { Id = Guid.NewGuid(), Name = "Docker", Category = Category.Technical };
            _context.Skills.Add(skill2);

            _context.JobSkills.AddRange(
                new JobSkill { Id = Guid.NewGuid(), JobPostingId = job.Id, SkillId = skill1.Id },
                new JobSkill { Id = Guid.NewGuid(), JobPostingId = job.Id, SkillId = skill2.Id }
            );
            await _context.SaveChangesAsync();

            await _service.RemoveAllForJobPostingAsync(job.Id);

            _context.ChangeTracker.Clear();
            var remaining = await _context.JobSkills
                .Where(js => js.JobPostingId == job.Id)
                .ToListAsync();

            remaining.Should().BeEmpty();
        }
    }
}