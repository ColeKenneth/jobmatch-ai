using JobMatchAI.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.Services.Interfaces
{
    public interface IAlumniService
    {
        Task<AlumniDto?> GetByIdAsync(Guid id);
        Task<AlumniDto?> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<AlumniDto>> GetAllAsync();
        Task<AlumniDto> CreateAsync(AlumniDto dto);
        Task UpdateAsync(Guid id, AlumniDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
