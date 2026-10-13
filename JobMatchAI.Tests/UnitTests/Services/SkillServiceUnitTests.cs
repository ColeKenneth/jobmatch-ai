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
    public class SkillServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<ILogger<SkillService>> _loggerMock;
        private readonly SkillService _service;

        private readonly List<Skill> _skills = [];

        public SkillServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);
            _loggerMock = new Mock<ILogger<SkillService>>();

            _contextMock.Setup(c => c.Skills).Returns(DbSetMockHelper.CreateMockDbSet(_skills));

            _service = new SkillService(_contextMock.Object, _loggerMock.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _skills.Add(new Skill
            {
                Id = Guid.NewGuid(),
                Name = "C#",
                Category = Category.Technical
            });

            _skills.Add(new Skill
            {
                Id = Guid.NewGuid(),
                Name = "Python",
                Category = Category.Technical
            });
        }

        [Fact]
        public async Task CreateAsync_ReturnsDto_WhenValidRequest()
        {
            var dto = new SkillDto
            {
                Name = "JavaScript",
                Category = Category.Technical,
                OntologyId = "JS-001",
                Description = "Web scripting language",
                IconUrl = "https://example.com/js.png"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.Name.Should().Be("JavaScript");
            result.Category.Should().Be(Category.Technical);
            result.OntologyId.Should().Be("JS-001");
            result.Description.Should().Be("Web scripting language");
            result.IconUrl.Should().Be("https://example.com/js.png");
            result.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateAsync_TrimsName_WhenNameHasWhitespace()
        {
            var dto = new SkillDto
            {
                Name = "  Go  ",
                Category = Category.Technical
            };

            var result = await _service.CreateAsync(dto);

            result.Name.Should().Be("Go");
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenNameExists()
        {
            var dto = new SkillDto
            {
                Name = "C#",
                Category = Category.Technical
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ThrowsAlreadyExistsException_WhenNameDiffersOnlyByCase()
        {
            var dto = new SkillDto
            {
                Name = "c#",
                Category = Category.Technical
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenExists()
        {
            var id = _skills[0].Id;

            var result = await _service.ExistsAsync(id);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalse_WhenDoesNotExist()
        {
            var result = await _service.ExistsAsync(Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllSkillsOrderedByName()
        {
            var result = (await _service.GetAllAsync()).ToList();

            result.Should().HaveCount(2);
            result[0].Name.Should().Be("C#");
            result[1].Name.Should().Be("Python");
        }

        [Fact]
        public async Task GetByCategoryAsync_ReturnsOnlyMatchingCategory()
        {
            _skills.Add(new Skill
            {
                Id = Guid.NewGuid(),
                Name = "Communication",
                Category = Category.Soft
            });

            var result = (await _service.GetByCategoryAsync(Category.Technical)).ToList();

            result.Should().HaveCount(2);
            result.Should().OnlyContain(s => s.Category == Category.Technical);
        }

        [Fact]
        public async Task GetByCategoryAsync_ReturnsEmpty_WhenNoMatches()
        {
            var result = await _service.GetByCategoryAsync(Category.Language);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsDto_WhenExists()
        {
            var id = _skills[0].Id;

            var result = await _service.GetByIdAsync(id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.Name.Should().Be("C#");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsDto_WhenExists()
        {
            var result = await _service.GetByNameAsync("C#");

            result.Should().NotBeNull();
            result!.Name.Should().Be("C#");
        }

        [Fact]
        public async Task GetByNameAsync_IsCaseInsensitive()
        {
            var result = await _service.GetByNameAsync("c#");

            result.Should().NotBeNull();
            result!.Name.Should().Be("C#");
        }

        [Fact]
        public async Task GetByNameAsync_TrimsInput()
        {
            var result = await _service.GetByNameAsync("  Python  ");

            result.Should().NotBeNull();
            result!.Name.Should().Be("Python");
        }

        [Fact]
        public async Task GetByNameAsync_ReturnsNull_WhenDoesNotExist()
        {
            var result = await _service.GetByNameAsync("NonExistent");

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields_WhenValidRequest()
        {
            var skill = _skills[0];

            var dto = new SkillDto
            {
                Name = "C# 12",
                Category = Category.Technical,
                OntologyId = "CS-002",
                Description = "Updated description",
                IconUrl = "https://example.com/cs12.png"
            };

            await _service.UpdateAsync(skill.Id, dto);

            var updated = _skills.First(s => s.Id == skill.Id);
            updated.Name.Should().Be("C# 12");
            updated.OntologyId.Should().Be("CS-002");
            updated.Description.Should().Be("Updated description");
            updated.IconUrl.Should().Be("https://example.com/cs12.png");
        }

        [Fact]
        public async Task UpdateAsync_ThrowsAlreadyExistsException_WhenNameConflicts()
        {
            var skill = _skills[0];

            var dto = new SkillDto
            {
                Name = "Python",
                Category = Category.Technical
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.UpdateAsync(skill.Id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsAlreadyExistsException_WhenNameConflictsIgnoringCase()
        {
            var skill = _skills[0];

            var dto = new SkillDto
            {
                Name = "python",
                Category = Category.Technical
            };

            await Assert.ThrowsAsync<AlreadyExistsException>(() => _service.UpdateAsync(skill.Id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsNotFoundException_WhenDoesNotExist()
        {
            var dto = new SkillDto
            {
                Name = "AnySkill",
                Category = Category.Technical
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), dto));
        }
    }
}