using JobMatchAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IEmployerService
    {
        Task<EmployerDto?> GetByIdAsync(Guid id);
        Task<EmployerDto?> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<EmployerDto>> GetAllAsync();
        Task<EmployerDto> CreateAsync(EmployerDto dto);
        Task UpdateAsync(Guid id, EmployerDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
