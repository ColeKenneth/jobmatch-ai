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
    public class AlumniServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<DbSet<User>> _usersDbSetMock;
        private readonly Mock<DbSet<Alumni>> _alumniDbSetMock;
        private readonly Mock<ILogger<AlumniService>> _loggerMock;
        private readonly AlumniService _service;

        private readonly List<User> _users = [];
        private readonly List<Alumni> _alumni = [];

        public AlumniServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);

            _loggerMock = new Mock<ILogger<AlumniService>>();

            _usersDbSetMock = new Mock<DbSet<User>>();
            _alumniDbSetMock = new Mock<DbSet<Alumni>>();

            var usersQueryable = _users.AsQueryable();
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<User>(usersQueryable.Provider));
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.Expression).Returns(usersQueryable.Expression);
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(usersQueryable.ElementType);
            _usersDbSetMock.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(usersQueryable.GetEnumerator());
            _usersDbSetMock.As<IAsyncEnumerable<User>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<User>(usersQueryable.GetEnumerator()));

            _usersDbSetMock
                .Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _users.FirstOrDefault(u => u.Id == (Guid)ids[0]));

            var alumniQueryable = _alumni.AsQueryable();
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Alumni>(alumniQueryable.Provider));
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.Expression).Returns(alumniQueryable.Expression);
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.ElementType).Returns(alumniQueryable.ElementType);
            _alumniDbSetMock.As<IQueryable<Alumni>>().Setup(m => m.GetEnumerator()).Returns(alumniQueryable.GetEnumerator());
            _alumniDbSetMock.As<IAsyncEnumerable<Alumni>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Alumni>(alumniQueryable.GetEnumerator()));

            _alumniDbSetMock
                .Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _alumni.FirstOrDefault(a => a.Id == (Guid)ids[0]));

            _contextMock.Setup(c => c.Users).Returns(_usersDbSetMock.Object);
            _contextMock.Setup(c => c.Alumni).Returns(_alumniDbSetMock.Object);

            _service = new AlumniService(_contextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAlumniAndReturnDto_WhenUserExistsAndRoleIsAlumniAndProfileDoesNotExist()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Alumni });

            var dto = new AlumniDto
            {
                UserId = userId,
                Program = "BS Information Technology",
                GraduationDate = new DateOnly(2024, 6, 15),
                CurrentEmployer = "Tech Corp",
                CurrentPosition = "Software Engineer"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.Program.Should().Be("BS Information Technology");
            result.CurrentEmployer.Should().Be("Tech Corp");
            result.CurrentPosition.Should().Be("Software Engineer");

            _alumniDbSetMock.Verify(d => d.Add(It.IsAny<Alumni>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var dto = new AlumniDto
            {
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"*User {userId} not found*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenUserRoleIsNotAlumni()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Student });

            var dto = new AlumniDto
            {
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*User must have the alumni role*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenAlumniProfileAlreadyExists()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Alumni });
            _alumni.Add(new Alumni { Id = Guid.NewGuid(), UserId = userId, Program = "BSIT", GraduationDate = new DateOnly(2023, 6, 15) });

            var dto = new AlumniDto
            {
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage($"*An alumni already exists for user {userId}*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenDbUpdateExceptionOccurs()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Alumni });

            var dto = new AlumniDto
            {
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Failed to create alumni*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnAlumniDto_WhenProfileExists()
        {
            var alumniId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _alumni.Add(new Alumni
            {
                Id = alumniId,
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            });

            var result = await _service.GetByIdAsync(alumniId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(alumniId);
            result.Program.Should().Be("BSIT");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenProfileDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnAlumniDto_WhenProfileExists()
        {
            var alumniId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _alumni.Add(new Alumni
            {
                Id = alumniId,
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            });

            var result = await _service.GetByUserIdAsync(userId);

            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
            result.Program.Should().Be("BSIT");
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnNull_WhenProfileDoesNotExist()
        {
            var result = await _service.GetByUserIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnCollectionOfAlumniDtos_OrderedByGraduationDate()
        {
            _alumni.Add(new Alumni { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Program = "BSIT", GraduationDate = new DateOnly(2025, 6, 15) });
            _alumni.Add(new Alumni { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Program = "BSCS", GraduationDate = new DateOnly(2023, 6, 15) });

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
            result.First().GraduationDate.Should().Be(new DateOnly(2023, 6, 15));
            result.Last().GraduationDate.Should().Be(new DateOnly(2025, 6, 15));
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenProfileExists()
        {
            var alumniId = Guid.NewGuid();
            _alumni.Add(new Alumni { Id = alumniId, Program = "BSIT", GraduationDate = new DateOnly(2024, 6, 15) });

            var exists = await _service.ExistsAsync(alumniId);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenProfileDoesNotExist()
        {
            var exists = await _service.ExistsAsync(Guid.NewGuid());

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateAlumniProfile_WhenDataIsValid()
        {
            var alumniId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingAlumni = new Alumni
            {
                Id = alumniId,
                UserId = userId,
                Program = "Old Program",
                GraduationDate = new DateOnly(2023, 6, 15)
            };
            _alumni.Add(existingAlumni);

            var dto = new AlumniDto
            {
                Id = alumniId,
                UserId = userId,
                Program = "New Program",
                GraduationDate = new DateOnly(2024, 6, 15),
                CurrentEmployer = "New Corp",
                CurrentPosition = "Lead Dev"
            };

            await _service.UpdateAsync(alumniId, dto);

            existingAlumni.Program.Should().Be("New Program");
            existingAlumni.CurrentEmployer.Should().Be("New Corp");
            existingAlumni.CurrentPosition.Should().Be("Lead Dev");
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenProfileDoesNotExist()
        {
            var dto = new AlumniDto { Program = "BSIT" };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenUserIdIsReassigned()
        {
            var alumniId = Guid.NewGuid();
            var originalUserId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();

            _alumni.Add(new Alumni
            {
                Id = alumniId,
                UserId = originalUserId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            });

            var dto = new AlumniDto
            {
                UserId = differentUserId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            };

            Func<Task> act = async () => await _service.UpdateAsync(alumniId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign alumni profile to a different user*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenDbUpdateExceptionOccurs()
        {
            var alumniId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _alumni.Add(new Alumni
            {
                Id = alumniId,
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            });

            var dto = new AlumniDto
            {
                UserId = userId,
                Program = "BSIT",
                GraduationDate = new DateOnly(2024, 6, 15)
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.UpdateAsync(alumniId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Failed to update alumni profile*");
        }
    }
}