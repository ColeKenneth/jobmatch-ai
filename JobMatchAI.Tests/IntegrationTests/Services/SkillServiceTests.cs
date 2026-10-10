using FluentAssertions;
using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Implementations;
using JobMatchAI.Application.Services.Interfaces;
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
    public class SkillServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SkillService _service;

        public SkillServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new SkillService(_context, Mock.Of<ILogger<SkillService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddSkill_WhenSkillIsUnique()
        {
            var dto = new SkillDto
            {
                Name = "C# Programming",
                Category = Category.Technical,
                Description = "Backend development skill"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.Name.Should().Be("C# Programming");

            var savedSkill = await _context.Skills.FindAsync(result.Id);
            savedSkill.Should().NotBeNull();
            savedSkill!.Name.Should().Be("C# Programming");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenNameCaseInsensitiveExists()
        { 
            _context.Skills.Add(new Skill
            {
                Name = "C#",
                Category = Category.Technical
            });

            await _context.SaveChangesAsync();

            var dto = new SkillDto
            {
                Name = "c#",
                Category = Category.Technical
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnSkillDto_WhenExists()
        {
            var skill = new Skill { Id = Guid.NewGuid(), Name = "Python", Category = Category.Technical };
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(skill.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(skill.Id);
            result.Name.Should().Be("Python");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateSkill_WhenNameIsUnique()
        {
            var skill = new Skill { Id = Guid.NewGuid(), Name = "ReactJS", Category = Category.Technical };
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            var updateDto = new SkillDto
            {
                Id = skill.Id,
                Name = " React Native ",
                Category = Category.Technical,
                Description = "Mobile framework"
            };

            await _service.UpdateAsync(skill.Id, updateDto);

            var updatedSkill = await _context.Skills.FindAsync(skill.Id);
            updatedSkill.Should().NotBeNull();
            updatedSkill!.Name.Should().Be("React Native");
            updatedSkill.Description.Should().Be("Mobile framework");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowAlreadyExistsException_WhenNewNameConflictsWithAnotherSkill()
        {
            var existingSkill1 = new Skill { Id = Guid.NewGuid(), Name = "TypeScript", Category = Category.Technical };
            var existingSkill2 = new Skill { Id = Guid.NewGuid(), Name = "JavaScript", Category = Category.Technical };
            _context.Skills.AddRange(existingSkill1, existingSkill2);
            await _context.SaveChangesAsync();

            var updateDto = new SkillDto
            {
                Id = existingSkill2.Id,
                Name = "typescript", 
                Category = Category.Technical
            };

            Func<Task> act = async () => await _service.UpdateAsync(existingSkill2.Id, updateDto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenSkillDoesNotExist()
        {
            var dto = new SkillDto { Name = "NonExistentSkill", Category = Category.Technical };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveSkill_WhenSkillExists()
        {
            var skill = new Skill { Id = Guid.NewGuid(), Name = "Docker", Category = Category.Technical };
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(skill.Id);

            _context.ChangeTracker.Clear();

            var deletedSkill = await _context.Skills.FindAsync(skill.Id);
            deletedSkill.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenSkillDoesNotExist()
        {
            var nonExistentId = Guid.NewGuid();

            Func<Task> act = async () => await _service.DeleteAsync(nonExistentId);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
