using JobMatchAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IEmploymentHistoryService
    {
        Task<EmploymentHistoryDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<EmploymentHistoryDto>> GetByAlumniAsync(Guid alumniId);
        Task<EmploymentHistoryDto> CreateAsync(Guid alumniId, EmploymentHistoryDto dto);
        Task UpdateAsync(Guid id, EmploymentHistoryDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
