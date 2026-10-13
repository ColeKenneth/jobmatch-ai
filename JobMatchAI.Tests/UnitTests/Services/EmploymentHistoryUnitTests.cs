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
    public class EmploymentHistoryServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<DbSet<Alumni>> _alumniDbSetMock;
        private readonly Mock<DbSet<EmploymentHistory>> _employmentHistoriesDbSetMock;
        private readonly Mock<ILogger<EmploymentHistoryService>> _loggerMock;
        private readonly EmploymentHistoryService _service;

        private readonly List<Alumni> _alumni = [];
        private readonly List<EmploymentHistory> _employmentHistories = [];

        public EmploymentHistoryServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);

            _loggerMock = new Mock<ILogger<EmploymentHistoryService>>();

            _alumniDbSetMock = new Mock<DbSet<Alumni>>();
            _employmentHistoriesDbSetMock = new Mock<DbSet<EmploymentHistory>>();

            var alumniQueryable = _alumni.AsQueryable();
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Alumni>(alumniQueryable.Provider));
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.Expression).Returns(alumniQueryable.Expression);
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.ElementType).Returns(alumniQueryable.ElementType);
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.GetEnumerator()).Returns(alumniQueryable.GetEnumerator());
            _alumniDbSetMock.As<IAsyncEnumerable<Alumni>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Alumni>(alumniQueryable.GetEnumerator()));

            var employmentHistoriesQueryable = _employmentHistories.AsQueryable();
            _employmentHistoriesDbSetMock.As<IQueryable<EmploymentHistory>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<EmploymentHistory>(employmentHistoriesQueryable.Provider));
            _employmentHistoriesDbSetMock.As<IQueryable<EmploymentHistory>>().Setup(m => m.Expression).Returns(employmentHistoriesQueryable.Expression);
            _employmentHistoriesDbSetMock.As<IQueryable<EmploymentHistory>>().Setup(m => m.ElementType).Returns(employmentHistoriesQueryable.ElementType);
            _employmentHistoriesDbSetMock.As<IQueryable<EmploymentHistory>>().Setup(m => m.GetEnumerator()).Returns(employmentHistoriesQueryable.GetEnumerator());
            _employmentHistoriesDbSetMock.As<IAsyncEnumerable<EmploymentHistory>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<EmploymentHistory>(employmentHistoriesQueryable.GetEnumerator()));

            _employmentHistoriesDbSetMock
                .Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _employmentHistories.FirstOrDefault(eh => eh.Id == (Guid)ids[0]));

            _contextMock.Setup(c => c.Alumni).Returns(_alumniDbSetMock.Object);
            _contextMock.Setup(c => c.EmploymentHistories).Returns(_employmentHistoriesDbSetMock.Object);

            _service = new EmploymentHistoryService(_contextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateEmploymentHistoryAndReturnDto_WhenAlumniExistsAndDatesAreValid()
        {
            var alumniId = Guid.NewGuid();
            _alumni.Add(new Alumni { Id = alumniId });

            var dto = new EmploymentHistoryDto
            {
                AlumniId = alumniId,
                CompanyName = "Tech Corp",
                Position = "Software Engineer",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };

            var result = await _service.CreateAsync(alumniId, dto);

            result.Should().NotBeNull();
            result.CompanyName.Should().Be("Tech Corp");
            result.AlumniId.Should().Be(alumniId);

            _employmentHistoriesDbSetMock.Verify(d => d.Add(It.IsAny<EmploymentHistory>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenAlumniDoesNotExist()
        {
            var alumniId = Guid.NewGuid();
            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Software Engineer",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.CreateAsync(alumniId, dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"*Alumni {alumniId} not found*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenCurrentPositionHasEndDate()
        {
            var alumniId = Guid.NewGuid();
            _alumni.Add(new Alumni { Id = alumniId });

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Engineer",
                StartDate = new DateOnly(2022, 1, 1),
                EndDate = new DateOnly(2023, 1, 1),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.CreateAsync(alumniId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*A current position cannot have an end date*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenPastPositionMissingEndDate()
        {
            var alumniId = Guid.NewGuid();
            _alumni.Add(new Alumni { Id = alumniId });

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Engineer",
                StartDate = new DateOnly(2022, 1, 1),
                EndDate = null,
                IsCurrent = false
            };

            Func<Task> act = async () => await _service.CreateAsync(alumniId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*A past position must have an end date*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenEndDateBeforeStartDate()
        {
            var alumniId = Guid.NewGuid();
            _alumni.Add(new Alumni { Id = alumniId });

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Engineer",
                StartDate = new DateOnly(2023, 1, 1),
                EndDate = new DateOnly(2022, 1, 1),
                IsCurrent = false
            };

            Func<Task> act = async () => await _service.CreateAsync(alumniId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*End date must be on or after the start date*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenDbUpdateExceptionOccurs()
        {
            var alumniId = Guid.NewGuid();
            _alumni.Add(new Alumni { Id = alumniId });

            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Tech Corp",
                Position = "Engineer",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.CreateAsync(alumniId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Failed to create employment history*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEmploymentHistoryDto_WhenExists()
        {
            var id = Guid.NewGuid();
            _employmentHistories.Add(new EmploymentHistory
            {
                Id = id,
                CompanyName = "Tech Corp",
                Position = "Engineer",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            });

            var result = await _service.GetByIdAsync(id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.CompanyName.Should().Be("Tech Corp");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByAlumniAsync_ShouldReturnCollectionOrderedByStartDateDescending()
        {
            var alumniId = Guid.NewGuid();
            _employmentHistories.Add(new EmploymentHistory { Id = Guid.NewGuid(), AlumniId = alumniId, CompanyName = "Old", StartDate = new DateOnly(2020, 1, 1), IsCurrent = false, EndDate = new DateOnly(2021, 1, 1) });
            _employmentHistories.Add(new EmploymentHistory { Id = Guid.NewGuid(), AlumniId = alumniId, CompanyName = "New", StartDate = new DateOnly(2022, 1, 1), IsCurrent = true });

            var result = await _service.GetByAlumniAsync(alumniId);

            result.Should().HaveCount(2);
            result.First().CompanyName.Should().Be("New");
            result.Last().CompanyName.Should().Be("Old");
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
        {
            var id = Guid.NewGuid();
            _employmentHistories.Add(new EmploymentHistory { Id = id, CompanyName = "Tech Corp", StartDate = new DateOnly(2022, 1, 1) });

            var exists = await _service.ExistsAsync(id);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenDoesNotExist()
        {
            var exists = await _service.ExistsAsync(Guid.NewGuid());
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEmploymentHistory_WhenDataIsValid()
        {
            var id = Guid.NewGuid();
            var alumniId = Guid.NewGuid();
            var history = new EmploymentHistory
            {
                Id = id,
                AlumniId = alumniId,
                CompanyName = "Old Name",
                Position = "Old Pos",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };
            _employmentHistories.Add(history);

            var dto = new EmploymentHistoryDto
            {
                AlumniId = alumniId,
                CompanyName = "New Name",
                Position = "New Pos",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };

            await _service.UpdateAsync(id, dto);

            history.CompanyName.Should().Be("New Name");
            history.Position.Should().Be("New Pos");
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            var dto = new EmploymentHistoryDto
            {
                CompanyName = "Name",
                Position = "Pos",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenAlumniIdReassigned()
        {
            var id = Guid.NewGuid();
            var originalAlumniId = Guid.NewGuid();
            var newAlumniId = Guid.NewGuid();

            _employmentHistories.Add(new EmploymentHistory
            {
                Id = id,
                AlumniId = originalAlumniId,
                CompanyName = "Tech Corp",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            });

            var dto = new EmploymentHistoryDto
            {
                AlumniId = newAlumniId,
                CompanyName = "Tech Corp",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };

            Func<Task> act = async () => await _service.UpdateAsync(id, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign employment history to a different alumni*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenDbUpdateExceptionOccurs()
        {
            var id = Guid.NewGuid();
            var alumniId = Guid.NewGuid();
            _employmentHistories.Add(new EmploymentHistory
            {
                Id = id,
                AlumniId = alumniId,
                CompanyName = "Tech Corp",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            });

            var dto = new EmploymentHistoryDto
            {
                AlumniId = alumniId,
                CompanyName = "Tech Corp",
                StartDate = new DateOnly(2022, 1, 1),
                IsCurrent = true
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.UpdateAsync(id, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Failed to update employment history*");
        }
    }
}