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
    public class EmployerServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly EmployerService _service;

        public EmployerServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new EmployerService(_context, Mock.Of<ILogger<EmployerService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<User> CreateTestUserAsync(UserRole role = UserRole.Employer, string? email = null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email ?? $"employer_{Guid.NewGuid()}@test.com",
                PasswordHash = "hash",
                Role = role
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task CreateAsync_Should_CreateEmployer_When_ValidDtoAndEmployerUser()
        {
            var user = await CreateTestUserAsync(UserRole.Employer);
            var dto = new EmployerDto
            {
                UserId = user.Id,
                CompanyName = "Tech Corp",
                Industry = "Software",
                Website = "https://techcorp.com",
                CompanyDescription = "Leading tech solutions"
            };

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be(user.Id);
            result.CompanyName.Should().Be("Tech Corp");
            result.Industry.Should().Be("Software");

            var saved = await _context.Employers.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_Should_ThrowNotFoundException_When_UserDoesNotExist()
        {
            var dto = new EmployerDto
            {
                UserId = Guid.NewGuid(),
                CompanyName = "Tech Corp",
                Industry = "Software"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*User not found*");
        }

        [Fact]
        public async Task CreateAsync_Should_ThrowBusinessRuleException_When_UserIsNotEmployer()
        {
            var user = await CreateTestUserAsync(UserRole.Student);
            var dto = new EmployerDto
            {
                UserId = user.Id,
                CompanyName = "Tech Corp",
                Industry = "Software"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*User must have the employer role*");
        }

        [Fact]
        public async Task CreateAsync_Should_ThrowAlreadyExistsException_When_EmployerProfileAlreadyExistsForUser()
        {
            var user = await CreateTestUserAsync(UserRole.Employer);
            _context.Employers.Add(new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = "Existing Corp",
                Industry = "Tech"
            });
            await _context.SaveChangesAsync();

            var dto = new EmployerDto
            {
                UserId = user.Id,
                CompanyName = "New Corp",
                Industry = "Software"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*An employer profile already exists*");
        }

        [Fact]
        public async Task CreateAsync_Should_ThrowAlreadyExistsException_When_CompanyNameAlreadyExistsCaseInsensitive()
        {
            var user1 = await CreateTestUserAsync(UserRole.Employer);
            var user2 = await CreateTestUserAsync(UserRole.Employer);

            _context.Employers.Add(new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                CompanyName = "Tech Corp",
                Industry = "Tech"
            });
            await _context.SaveChangesAsync();

            var dto = new EmployerDto
            {
                UserId = user2.Id,
                CompanyName = "tech corp",
                Industry = "Software"
            };

            Func<Task> act = async () => await _service.CreateAsync(dto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task DeleteAsync_Should_DeleteEmployer_When_EmployerExists()
        {
            var user = await CreateTestUserAsync(UserRole.Employer);
            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = "Tech Corp",
                Industry = "Software"
            };
            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(employer.Id);

            _context.ChangeTracker.Clear();
            var deleted = await _context.Employers.FindAsync(employer.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_Should_ThrowNotFoundException_When_EmployerDoesNotExist()
        {
            Func<Task> act = async () => await _service.DeleteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnEmployerDto_When_EmployerExists()
        {
            var user = await CreateTestUserAsync(UserRole.Employer);
            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = "Tech Corp",
                Industry = "Software"
            };
            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(employer.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(employer.Id);
            result.CompanyName.Should().Be("Tech Corp");
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnNull_When_EmployerDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserIdAsync_Should_ReturnEmployerDto_When_UserIdExists()
        {
            var user = await CreateTestUserAsync(UserRole.Employer);
            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = "Tech Corp",
                Industry = "Software"
            };
            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();

            var result = await _service.GetByUserIdAsync(user.Id);

            result.Should().NotBeNull();
            result!.UserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task UpdateAsync_Should_UpdateEmployer_When_ValidDtoAndUniqueCompanyName()
        {
            var user = await CreateTestUserAsync(UserRole.Employer);
            var employer = new Employer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = "Old Name",
                Industry = "Old Industry"
            };
            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();

            var updateDto = new EmployerDto
            {
                UserId = user.Id,
                CompanyName = "New Name",
                Industry = "New Industry"
            };

            await _service.UpdateAsync(employer.Id, updateDto);

            var updated = await _context.Employers.FindAsync(employer.Id);
            updated.Should().NotBeNull();
            updated!.CompanyName.Should().Be("New Name");
            updated.Industry.Should().Be("New Industry");
        }

        [Fact]
        public async Task UpdateAsync_Should_ThrowNotFoundException_When_EmployerDoesNotExist()
        {
            var updateDto = new EmployerDto
            {
                CompanyName = "Tech Corp",
                Industry = "Software"
            };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), updateDto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateAsync_Should_ThrowAlreadyExistsException_When_CompanyNameConflictsWithAnotherEmployer()
        {
            var user1 = await CreateTestUserAsync(UserRole.Employer);
            var user2 = await CreateTestUserAsync(UserRole.Employer);

            var emp1 = new Employer { Id = Guid.NewGuid(), UserId = user1.Id, CompanyName = "Company A", Industry = "Tech" };
            var emp2 = new Employer { Id = Guid.NewGuid(), UserId = user2.Id, CompanyName = "Company B", Industry = "Tech" };
            _context.Employers.AddRange(emp1, emp2);
            await _context.SaveChangesAsync();

            var updateDto = new EmployerDto
            {
                UserId = user2.Id,
                CompanyName = "company a",
                Industry = "Tech"
            };

            Func<Task> act = async () => await _service.UpdateAsync(emp2.Id, updateDto);

            await act.Should().ThrowAsync<AlreadyExistsException>()
                .WithMessage("*already exists*");
        }
    }
}