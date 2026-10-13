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
    public class CertificationServiceUnitTests
    {
        private readonly Mock<AppDbContext> _contextMock;
        private readonly Mock<DbSet<Certification>> _certificationsDbSetMock;
        private readonly Mock<ILogger<CertificationService>> _loggerMock;
        private readonly CertificationService _service;

        private readonly List<Student> _students = [];
        private readonly List<Certification> _certifications = [];

        public CertificationServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().Options;
            _contextMock = new Mock<AppDbContext>(options);

            _loggerMock = new Mock<ILogger<CertificationService>>();

            _certificationsDbSetMock = new Mock<DbSet<Certification>>();

            var certQueryable = _certifications.AsQueryable();
            _certificationsDbSetMock.As<IQueryable<Certification>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Certification>(certQueryable.Provider));
            _certificationsDbSetMock.As<IQueryable<Certification>>().Setup(m => m.Expression).Returns(certQueryable.Expression);
            _certificationsDbSetMock.As<IQueryable<Certification>>().Setup(m => m.ElementType).Returns(certQueryable.ElementType);
            _certificationsDbSetMock.As<IQueryable<Certification>>().Setup(m => m.GetEnumerator()).Returns(certQueryable.GetEnumerator());
            _certificationsDbSetMock.As<IAsyncEnumerable<Certification>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Certification>(certQueryable.GetEnumerator()));

            _certificationsDbSetMock
                .Setup(d => d.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _certifications.FirstOrDefault(c => c.Id == (Guid)ids[0]));

            _contextMock.Setup(c => c.Students).Returns(DbSetMockHelper.CreateMockDbSet(_students.AsQueryable()));
            _contextMock.Setup(c => c.Certifications).Returns(_certificationsDbSetMock.Object);

            _service = new CertificationService(_contextMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateCertificationAndReturnDto_WhenStudentExistsAndDatesAreValid()
        {
            var studentId = Guid.NewGuid();
            _students.Add(new Student { Id = studentId });

            var dto = new CertificationDto
            {
                Name = "AWS Certified Developer",
                IssuingAuthority = "Amazon Web Services",
                DateIssued = DateTime.UtcNow.AddMonths(-1),
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                VerificationUrl = "https://aws.amazon.com/verify",
                CredentialId = "AWS-12345"
            };

            var result = await _service.CreateAsync(studentId, dto);

            result.Should().NotBeNull();
            result.Name.Should().Be("AWS Certified Developer");
            result.IssuingAuthority.Should().Be("Amazon Web Services");
            result.StudentId.Should().Be(studentId);

            _certificationsDbSetMock.Verify(d => d.Add(It.IsAny<Certification>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
        {
            var studentId = Guid.NewGuid();
            var dto = new CertificationDto
            {
                Name = "Azure Developer",
                IssuingAuthority = "Microsoft",
                DateIssued = DateTime.UtcNow.AddMonths(-1)
            };

            Func<Task> act = async () => await _service.CreateAsync(studentId, dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"*Student {studentId} not found*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowBusinessRuleException_WhenExpiryDateIsBeforeOrEqualDateIssued()
        {
            var studentId = Guid.NewGuid();
            _students.Add(new Student { Id = studentId });

            var issuedDate = DateTime.UtcNow;
            var dto = new CertificationDto
            {
                Name = "Bad Dates Cert",
                IssuingAuthority = "Test Authority",
                DateIssued = issuedDate,
                ExpiryDate = issuedDate.AddDays(-1)
            };

            Func<Task> act = async () => await _service.CreateAsync(studentId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Expiry date must be after the issued date*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCertificationDto_WhenCertificationExists()
        {
            var certId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            _certifications.Add(new Certification
            {
                Id = certId,
                StudentId = studentId,
                Name = "Java Certification",
                IssuingAuthority = "Oracle",
                DateIssued = DateTime.UtcNow.AddMonths(-6)
            });

            var result = await _service.GetByIdAsync(certId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(certId);
            result.Name.Should().Be("Java Certification");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenCertificationDoesNotExist()
        {
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByStudentAsync_ShouldReturnCollectionOfDtos_WhenStudentHasCertifications()
        {
            var studentId = Guid.NewGuid();
            _certifications.Add(new Certification { Id = Guid.NewGuid(), StudentId = studentId, Name = "Cert 1", IssuingAuthority = "Auth 1" });
            _certifications.Add(new Certification { Id = Guid.NewGuid(), StudentId = studentId, Name = "Cert 2", IssuingAuthority = "Auth 2" });
            _certifications.Add(new Certification { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), Name = "Other Cert", IssuingAuthority = "Other" });

            var result = await _service.GetByStudentAsync(studentId);

            result.Should().HaveCount(2);
            result.All(c => c.StudentId == studentId).Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenCertificationExists()
        {
            var certId = Guid.NewGuid();
            _certifications.Add(new Certification { Id = certId, Name = "Test" });

            var exists = await _service.ExistsAsync(certId);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenCertificationDoesNotExist()
        {
            var exists = await _service.ExistsAsync(Guid.NewGuid());

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateCertification_WhenDataIsValid()
        {
            var certId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var existingCert = new Certification
            {
                Id = certId,
                StudentId = studentId,
                Name = "Old Name",
                IssuingAuthority = "Old Authority",
                DateIssued = DateTime.UtcNow.AddMonths(-2)
            };
            _certifications.Add(existingCert);

            var dto = new CertificationDto
            {
                Id = certId,
                StudentId = studentId,
                Name = "Updated Name",
                IssuingAuthority = "Updated Authority",
                DateIssued = DateTime.UtcNow.AddMonths(-2)
            };

            await _service.UpdateAsync(certId, dto);

            existingCert.Name.Should().Be("Updated Name");
            existingCert.IssuingAuthority.Should().Be("Updated Authority");
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenCertificationDoesNotExist()
        {
            var dto = new CertificationDto { Name = "Updated" };

            Func<Task> act = async () => await _service.UpdateAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*not found*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenExpiryDateIsInvalid()
        {
            var certId = Guid.NewGuid();
            var issuedDate = DateTime.UtcNow;
            var dto = new CertificationDto
            {
                DateIssued = issuedDate,
                ExpiryDate = issuedDate.AddDays(-1)
            };

            Func<Task> act = async () => await _service.UpdateAsync(certId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Expiry date must be after the date issued*");
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowBusinessRuleException_WhenStudentIdIsReassigned()
        {
            var certId = Guid.NewGuid();
            var originalStudentId = Guid.NewGuid();
            var differentStudentId = Guid.NewGuid();

            _certifications.Add(new Certification
            {
                Id = certId,
                StudentId = originalStudentId,
                Name = "Cert",
                DateIssued = DateTime.UtcNow.AddMonths(-1)
            });

            var dto = new CertificationDto
            {
                StudentId = differentStudentId,
                Name = "Cert",
                DateIssued = DateTime.UtcNow.AddMonths(-1)
            };

            Func<Task> act = async () => await _service.UpdateAsync(certId, dto);

            await act.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*Cannot reassign certification to a different student*");

            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}