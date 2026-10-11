using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace JobMatchAI.Application.Services.Implementations
{
    public class CertificationService(AppDbContext context, ILogger<CertificationService> logger) : ICertificationService
    {
        public async Task<CertificationDto> CreateAsync(Guid studentId, CertificationDto dto)
        {
            var studentExists = await context.Students.AnyAsync(s => s.Id == studentId);

            if (!studentExists) throw new NotFoundException($"Student {studentId} not found.");

            if (dto.ExpiryDate.HasValue && dto.ExpiryDate.Value <= dto.DateIssued)
            {
                throw new BusinessRuleException("Expiry date must be after the issued date.");
            }

            var certification = new Certification
            {
                StudentId = studentId,
                Name = dto.Name.Trim(),
                IssuingAuthority = dto.IssuingAuthority.Trim(),
                DateIssued = dto.DateIssued,
                ExpiryDate = dto.ExpiryDate,
                VerificationUrl = dto.VerificationUrl?.Trim(),
                CredentialId = dto.CredentialId?.Trim()
            };

            context.Certifications.Add(certification);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error creating certification for student {StudentId}", studentId);
                throw new BusinessRuleException("Failed to create certification.");
            }

            logger.LogInformation("Attached certification {CertificationId} to student {StudentId}", certification.Id, studentId);
            return MapToDto(certification);
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedCount = await context.Certifications
                .Where(c => c.Id == id)
                .ExecuteDeleteAsync();

            if (deletedCount == 0) throw new NotFoundException($"Certification {id} not found.");
            logger.LogInformation("Removed certification {CertificationId}", id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await context.Certifications.AnyAsync(c => c.Id == id);
        }

        public async Task<CertificationDto?> GetByIdAsync(Guid id)
        {
            return await context.Certifications
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(AsCertificationDto)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CertificationDto>> GetByStudentAsync(Guid studentId)
        {
            return await context.Certifications
                .AsNoTracking()
                .Where(c => c.StudentId == studentId)
                .Select(AsCertificationDto)
                .ToListAsync();
        }

        public async Task UpdateAsync(Guid id, CertificationDto dto)
        {
            if (dto.ExpiryDate.HasValue && dto.ExpiryDate.Value <= dto.DateIssued)
            {
                throw new BusinessRuleException("Expiry date must be after the date issued.");
            }

            var certification = await context.Certifications.FindAsync(id)
                ?? throw new NotFoundException($"Certification {id} not found.");

            if (dto.StudentId != Guid.Empty && dto.StudentId != certification.StudentId)
            {
                throw new BusinessRuleException("Cannot reassign certification to a different student.");
            }

            certification.Name = dto.Name.Trim();
            certification.IssuingAuthority = dto.IssuingAuthority.Trim();
            certification.DateIssued = dto.DateIssued;
            certification.ExpiryDate = dto.ExpiryDate;
            certification.VerificationUrl = dto.VerificationUrl?.Trim();
            certification.CredentialId = dto.CredentialId?.Trim();

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Error updating certification {Id}", id);
                throw new BusinessRuleException("Failed to update certification.");
            }

            logger.LogInformation("Updated certification {Id}", id);
        }

        private static readonly Expression<Func<Certification, CertificationDto>> AsCertificationDto = c => new CertificationDto
        {
            Id = c.Id,
            StudentId = c.StudentId,
            Name = c.Name,
            IssuingAuthority = c.IssuingAuthority,
            DateIssued = c.DateIssued,
            ExpiryDate = c.ExpiryDate,
            VerificationUrl = c.VerificationUrl,
            CredentialId = c.CredentialId
        };

        private static readonly Func<Certification, CertificationDto> MapToDto = AsCertificationDto.Compile();
    }
}
