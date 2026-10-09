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

namespace JobMatchAI.Tests.Services
{
    public class CertificationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly CertificationService _service;

        public CertificationServiceTests()
        {
            _context = TestDbContextFactory.CreateSqliteDbContext();
            _service = new CertificationService(_context, Mock.Of<ILogger<CertificationService>>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private async Task<Student> CreateTestStudentAsync()
        {
            var user = new User { Id = Guid.NewGuid(), Email = "certstudent@test.com", PasswordHash = "hash", Role = UserRole.Student };
            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentIdNumber = "23-IT99999",
                Program = "BSIT",
                YearLevel = YearLevel.Graduating,
                Gwa = 1.50m
            };

            _context.Users.Add(user);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        [Fact]
        public async Task CreateAsync_ShouldAddCertification_WhenStudentExistsAndValid()
        {
            var student = await CreateTestStudentAsync();

            var dto = new CertificationDto
            {
                Name = "AWS Certified Developer",
                IssuingAuthority = "Amazon Web Services",
                DateIssued = DateTime.UtcNow.AddMonths(-6),
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                VerificationUrl = "https://aws.amazon.com/verify",
                CredentialId = "AWS-12345"
            };

            var result = await _service.CreateAsync(student.Id, dto);

            result.Should().NotBeNull();
            result.StudentId.Should().Be(student.Id);
            result.Name.Should().Be("AWS Certified Developer");
            result.IssuingAuthority.Should().Be("Amazon Web Services");

            var saved = await _context.Certifications.FindAsync(result.Id);
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            var dto = new CertificationDto
            {
                Name = "Oracle Certified Associate",
                IssuingAuthority = "Oracle",
                DateIssued = DateTime.UtcNow.AddMonths(-1)
            };

            Func<Task> act = async () => await _service.CreateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Student*not found*");
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenExpiryDateIsBeforeOrEqualDateIssued()
        {
            var student = await CreateTestStudentAsync();
            var issuedDate = DateTime.UtcNow;

            var dto = new CertificationDto
            {
                Name = "Scrum Master",
                IssuingAuthority = "Scrum.org",
                DateIssued = issuedDate,
                ExpiryDate = issuedDate.AddDays(-1)
            };

            Func<Task> act = async () => await _service.CreateAsync(student.Id, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Expiry date must be after the issued date*");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCertificationDto_WhenExists()
        {
            var student = await CreateTestStudentAsync();
            var cert = new Certification
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Name = "Java SE Programmer",
                IssuingAuthority = "Oracle",
                DateIssued = DateTime.UtcNow.AddMonths(-3)
            };

            _context.Certifications.Add(cert);
            await _context.SaveChangesAsync();

            var result = await _service.GetByIdAsync(cert.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(cert.Id);
            result.Name.Should().Be("Java SE Programmer");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentAsync_ShouldReturnCertifications_WhenStudentExists()
        {
            var student = await CreateTestStudentAsync();
            _context.Certifications.AddRange(
                new Certification { Id = Guid.NewGuid(), StudentId = student.Id, Name = "Cert 1", IssuingAuthority = "Auth A", DateIssued = DateTime.UtcNow.AddMonths(-2) },
                new Certification { Id = Guid.NewGuid(), StudentId = student.Id, Name = "Cert 2", IssuingAuthority = "Auth B", DateIssued = DateTime.UtcNow.AddMonths(-1) }
            );
            await _context.SaveChangesAsync();

            var results = await _service.GetByStudentAsync(student.Id);

            results.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateCertification_WhenValid()
        {
            var student = await CreateTestStudentAsync();
            var cert = new Certification
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Name = "Old Name",
                IssuingAuthority = "Old Authority",
                DateIssued = DateTime.UtcNow.AddMonths(-5)
            };

            _context.Certifications.Add(cert);
            await _context.SaveChangesAsync();

            var updateDto = new CertificationDto
            {
                StudentId = student.Id,
                Name = "Updated Name",
                IssuingAuthority = "Updated Authority",
                DateIssued = DateTime.UtcNow.AddMonths(-5),
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            await _service.UpdateAsync(cert.Id, updateDto);

            var updated = await _context.Certifications.FindAsync(cert.Id);
            updated.Should().NotBeNull();
            updated!.Name.Should().Be("Updated Name");
            updated.IssuingAuthority.Should().Be("Updated Authority");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenReassigningStudent()
        {
            var student = await CreateTestStudentAsync();
            var cert = new Certification
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Name = "Security+",
                IssuingAuthority = "CompTIA",
                DateIssued = DateTime.UtcNow.AddMonths(-2)
            };

            _context.Certifications.Add(cert);
            await _context.SaveChangesAsync();

            var updateDto = new CertificationDto
            {
                StudentId = Guid.NewGuid(),
                Name = "Security+",
                IssuingAuthority = "CompTIA",
                DateIssued = DateTime.UtcNow.AddMonths(-2)
            };

            Func<Task> act = async () => await _service.UpdateAsync(cert.Id, updateDto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign certification to a different student*");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveCertification_WhenExists()
        {
            var student = await CreateTestStudentAsync();
            var cert = new Certification
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Name = "Network+",
                IssuingAuthority = "CompTIA",
                DateIssued = DateTime.UtcNow.AddMonths(-4)
            };

            _context.Certifications.Add(cert);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(cert.Id);

            _context.ChangeTracker.Clear();

            var deleted = await _context.Certifications.FindAsync(cert.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenDoesNotExist()
        {
            Func<Task> act = async () => await _service.DeleteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Certification*not found*");
        }
    }
}