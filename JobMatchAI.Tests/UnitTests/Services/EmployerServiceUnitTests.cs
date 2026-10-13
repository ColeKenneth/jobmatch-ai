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
    public class EmployerServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<DbSet<User>> _usersDbSetMock;
        private readonly Mock<DbSet<Employer>> _employersDbSetMock;
        private readonly Mock<ILogger<EmployerService>> _loggerMock;
        private readonly EmployerService _service;

        private readonly List<User> _users = [];
        private readonly List<Employer> _employers = [];

        public EmployerServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);

            _loggerMock = new Mock<ILogger<EmployerService>>();

            _usersDbSetMock = new Mock<DbSet<User>>();
            _employersDbSetMock = new Mock<DbSet<Employer>>();

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

            var employersQueryable = _employers.AsQueryable();
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Employer>(employersQueryable.Provider));
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.Expression).Returns(employersQueryable.Expression);
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.ElementType).Returns(employersQueryable.ElementType);
            _employersDbSetMock.As<IQueryable<Employer>>().Setup(m => m.GetEnumerator()).Returns(employersQueryable.GetEnumerator());
            _employersDbSetMock.As<IAsyncEnumerable<Employer>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Employer>(employersQueryable.GetEnumerator()));

            _employersDbSetMock
                .Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _employers.FirstOrDefault(e => e.Id == (Guid)ids[0]));

            _contextMock.Setup(c => c.Users).Returns(_usersDbSetMock.Object);
            _contextMock.Setup(c => c.Employers).Returns(_employersDbSetMock.Object);

            _service = new EmployerService(_contextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateEmployerAndReturnDto_WhenUserExistsAndRoleIsEmployerAndProfileAndCompanyDoNotExist()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Employer });

            var dto = new EmployerDto
            {
                UserId = userId,
                CompanyName = "Tech Innovators Inc.",
                Industry = "Information Technology",
                Website = "https://techinnovators.com",
                CompanyDescription = "Leading software development company.",
                LogoUrl = "https://techinnovators.com/logo.png"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.CompanyName.Should().Be("Tech Innovators Inc.");
            result.Industry.Should().Be("Information Technology");

            _employersDbSetMock.Verify(d => d.Add(It.IsAny<Employer>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var dto = new EmployerDto
            {
                UserId = userId,
                CompanyName = "Acme Corp",
                Industry = "Retail"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"*User not found: {userId}*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenUserRoleIsNotEmployer()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Student });

            var dto = new EmployerDto
            {
                UserId = userId,
                CompanyName = "Acme Corp",
                Industry = "Retail"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*User must have the employer role*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenEmployerProfileAlreadyExists()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Employer });
            _employers.Add(new Employer { Id = Guid.NewGuid(), UserId = userId, CompanyName = "Old Company", Industry = "Tech" });

            var dto = new EmployerDto
            {
                UserId = userId,
                CompanyName = "New Company",
                Industry = "Tech"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage($"*An employer profile already exists for user {userId}*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenCompanyAlreadyExists()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Employer });
            _employers.Add(new Employer { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CompanyName = "Acme Corp", Industry = "Tech" });

            var dto = new EmployerDto
            {
                UserId = userId,
                CompanyName = "acme corp",
                Industry = "Tech"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowAlreadyExistsException_WhenDbUpdateExceptionOccurs()
        {
            var userId = Guid.NewGuid();
            _users.Add(new User { Id = userId, Role = UserRole.Employer });

            var dto = new EmployerDto
            {
                UserId = userId,
                CompanyName = "Acme Corp",
                Industry = "Tech"
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenEmployerExists()
        {
            var employerId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId, CompanyName = "Acme Corp", Industry = "Tech" });

            var exists = await _service.ExistsAsync(employerId);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenEmployerDoesNotExist()
        {
            var exists = await _service.ExistsAsync(Guid.NewGuid());

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnCollectionOfEmployerDtos()
        {
            _employers.Add(new Employer { Id = Guid.NewGuid(), CompanyName = "Company A", Industry = "Tech" });
            _employers.Add(new Employer { Id = Guid.NewGuid(), CompanyName = "Company B", Industry = "Finance" });

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEmployerDto_WhenEmployerExists()
        {
            var employerId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId, CompanyName = "Acme Corp", Industry = "Tech" });

            var result = await _service.GetByIdAsync(employerId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(employerId);
            result.CompanyName.Should().Be("Acme Corp");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenEmployerDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnEmployerDto_WhenEmployerExists()
        {
            var employerId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId, UserId = userId, CompanyName = "Acme Corp", Industry = "Tech" });

            var result = await _service.GetByUserIdAsync(userId);

            result.Should().NotBeNull();
            result!.UserId.Should().Be(userId);
            result.CompanyName.Should().Be("Acme Corp");
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnNull_WhenEmployerDoesNotExist()
        {
            var result = await _service.GetByUserIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEmployer_WhenDataIsValid()
        {
            var employerId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var existingEmployer = new Employer
            {
                Id = employerId,
                UserId = userId,
                CompanyName = "Old Name",
                Industry = "Old Industry"
            };
            _employers.Add(existingEmployer);

            var dto = new EmployerDto
            {
                Id = employerId,
                UserId = userId,
                CompanyName = "New Name",
                Industry = "New Industry"
            };

            await _service.UpdateAsync(employerId, dto);

            existingEmployer.CompanyName.Should().Be("New Name");
            existingEmployer.Industry.Should().Be("New Industry");
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowAlreadyExistsException_WhenCompanyNameAlreadyExistsForAnotherEmployer()
        {
            var employerId = Guid.NewGuid();
            var otherEmployerId = Guid.NewGuid();
            _employers.Add(new Employer { Id = otherEmployerId, CompanyName = "Existing Corp", Industry = "Tech" });
            _employers.Add(new Employer { Id = employerId, CompanyName = "My Corp", Industry = "Tech" });

            var dto = new EmployerDto
            {
                CompanyName = "existing corp",
                Industry = "Tech"
            };

            Func<Task> act = async () => await _service.UpdateAsync(employerId, dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenEmployerDoesNotExist()
        {
            var dto = new EmployerDto { CompanyName = "New Name", Industry = "Tech" };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowAlreadyExistsException_WhenDbUpdateExceptionOccurs()
        {
            var employerId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _employers.Add(new Employer { Id = employerId, UserId = userId, CompanyName = "Acme Corp", Industry = "Tech" });

            var dto = new EmployerDto
            {
                CompanyName = "Acme Corp",
                Industry = "Tech"
            };

            _contextMock
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Error"));

            Func<Task> act = async () => await _service.UpdateAsync(employerId, dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }
    }
}