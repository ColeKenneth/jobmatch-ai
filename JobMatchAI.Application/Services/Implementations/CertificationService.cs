using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Implementations
{
    public class CertificationService(AppDbContext context, ILogger<CertificationService> logger) : ICertificationService
    {
        public Task<CertificationDto> CreateAsync(Guid studentId, CertificationDto dto)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<CertificationDto?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CertificationDto>> GetByStudentAsync(Guid studentId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Guid id, CertificationDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
